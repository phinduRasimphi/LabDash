using LabDash.Areas.Identity.Data;
using LabDash.Enums;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class VerificationController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IVerifiedResultsNotificationService _notificationService;

        public VerificationController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IVerifiedResultsNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        // ============================================================
        // VERIFICATION QUEUE
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // LOAD ALL COMPLETED RESULTS THAT HAVE NOT BEEN VERIFIED
            // --------------------------------------------------------
            var results = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)

                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(req => req.Patient)

                .Include(r => r.CapturedByTechnician)

                .Where(r =>
                    r.Status == "Completed" &&
                    r.VerifiedByTechnicianId == null &&
                    r.TestRequestItem != null &&
                    r.TestRequestItem.Status == "Completed" &&
                    r.CapturedByTechnicianId != technician.Id
                )
                .OrderBy(r => r.DateCaptured)
                .ToListAsync();


            // --------------------------------------------------------
            // SEND INFORMATION TO VIEW
            // --------------------------------------------------------
            ViewBag.CurrentTechnicianId = technician.Id;

            return View(results);
        }


        // ============================================================
        // VERIFICATION HISTORY
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var item = await _context.TestRequestItems

                .Include(i => i.TestType)

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)

                .FirstOrDefaultAsync(
                    i => i.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();


            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .Include(r => r.VerifiedByTechnician)
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );


            var history = await _context.TestVerifications
                .Include(v => v.VerifiedByTechnician)
                .Where(v =>
                    v.TestRequestItemId == id
                )
                .OrderByDescending(v => v.VerificationDate)
                .ToListAsync();


            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.CurrentResult = result;

            return View(history);
        }


        // ============================================================
        // OPEN VERIFICATION PAGE
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Verify(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // LOAD TEST ITEM
            // --------------------------------------------------------
            var item = await _context.TestRequestItems

                .Include(i => i.TestType)

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(i => i.AssignedTechnician)

                .FirstOrDefaultAsync(
                    i => i.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();


            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------
            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result exists for this test.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // RESULT MUST BE COMPLETED
            // --------------------------------------------------------
            if (result.Status != "Completed")
            {
                TempData["Error"] =
                    "This result is not waiting for verification.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // TECHNICIAN CANNOT VERIFY OWN RESULT
            // --------------------------------------------------------
            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] =
                    "You cannot verify a result that you captured yourself.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // PREVENT DOUBLE VERIFICATION
            // --------------------------------------------------------
            if (!string.IsNullOrEmpty(
                result.VerifiedByTechnicianId))
            {
                TempData["Error"] =
                    "This result has already been verified.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // SEND DATA TO VIEW
            // --------------------------------------------------------
            ViewBag.TestItem = item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            ViewBag.Result =
                result;


            return View(new TestVerification
            {
                TestRequestItemId =
                    item.TestRequestItemId,

                Status =
                    "Verified"
            });
        }


        // ============================================================
        // SAVE VERIFICATION
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(
            TestVerification verification)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // VALIDATE ID
            // --------------------------------------------------------
            if (verification.TestRequestItemId <= 0)
            {
                TempData["Error"] =
                    "Invalid test request item.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // LOAD ITEM
            // --------------------------------------------------------
            var item = await _context.TestRequestItems

                .Include(i => i.TestType)

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)

                .FirstOrDefaultAsync(
                    i => i.TestRequestItemId ==
                         verification.TestRequestItemId
                );

            if (item == null)
                return NotFound();


            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------
            var result = await _context.TestResults
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId ==
                         verification.TestRequestItemId
                );

            if (result == null)
            {
                TempData["Error"] =
                    "Laboratory result could not be found.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // CHECK RESULT STATUS
            // --------------------------------------------------------
            if (result.Status != "Completed")
            {
                TempData["Error"] =
                    "This result is no longer waiting for verification.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // CANNOT VERIFY OWN RESULT
            // --------------------------------------------------------
            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] =
                    "You cannot verify your own laboratory result.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // PREVENT DOUBLE VERIFICATION
            // --------------------------------------------------------
            if (!string.IsNullOrEmpty(
                result.VerifiedByTechnicianId))
            {
                TempData["Error"] =
                    "This result has already been verified.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // VALIDATE DECISION
            // ========================================================

            if (verification.Status != "Verified" &&
                verification.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "Please select either Verify Result or Return For Review.";

                return RedirectToAction(
                    nameof(Verify),
                    new
                    {
                        id = verification.TestRequestItemId
                    });
            }


            // --------------------------------------------------------
            // REVIEW NOTE REQUIRED
            // --------------------------------------------------------
            if (verification.Status == "To Be Reviewed" &&
                string.IsNullOrWhiteSpace(
                    verification.VerificationNotes))
            {
                TempData["Error"] =
                    "Please provide a reason when returning a result for review.";

                return RedirectToAction(
                    nameof(Verify),
                    new
                    {
                        id = verification.TestRequestItemId
                    });
            }


            // ========================================================
            // CREATE VERIFICATION HISTORY RECORD
            // ========================================================

            var history = new TestVerification
            {
                TestRequestItemId =
                    item.TestRequestItemId,

                VerifiedByTechnicianId =
                    technician.Id,

                VerificationDate =
                    DateTime.Now,

                Status =
                    verification.Status,

                VerificationNotes =
                    verification.VerificationNotes
            };

            _context.TestVerifications.Add(history);


            // ========================================================
            // RESULT VERIFIED
            // ========================================================

            if (verification.Status == "Verified")
            {
                result.Status =
                    "Verified";

                result.VerifiedByTechnicianId =
                    technician.Id;

                result.VerificationDate =
                    DateTime.Now;

                result.VerificationNote =
                    verification.VerificationNotes;

                item.Status =
                    "Verified";
            }


            // ========================================================
            // RESULT RETURNED FOR REVIEW
            // ========================================================

            else
            {
                result.Status =
                    "To Be Reviewed";

                // VERY IMPORTANT:
                // This technician did NOT verify the result.
                result.VerifiedByTechnicianId =
                    null;

                result.VerificationDate =
                    null;

                result.VerificationNote =
                    verification.VerificationNotes;

                item.Status =
                    "To Be Reviewed";

                // Return the test to the technician
                // who originally captured it.
                item.AssignedTechnicianId =
                    result.CapturedByTechnicianId;
            }


            // ========================================================
            // SAVE
            // ========================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Verification could not be saved: " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // IF VERIFIED, CHECK ENTIRE REQUEST
            // ========================================================

            if (verification.Status == "Verified")
            {
                var allVerified =
                    await _context.TestRequestItems
                        .Where(x =>
                            x.RequestId ==
                            item.RequestId)
                        .AllAsync(x =>
                            x.Status == "Verified");


                if (allVerified)
                {
                    item.TestRequest.Status =
                        "Verified";

                    await _context.SaveChangesAsync();


                    // ------------------------------------------------
                    // NOTIFY DOCTOR
                    // ------------------------------------------------
                    try
                    {
                        await _notificationService
                            .SendVerifiedResultsAsync(
                                item.RequestId);
                    }
                    catch
                    {
                        // Do not break verification if
                        // email notification fails.
                    }
                }
            }


            // ========================================================
            // SUCCESS MESSAGE
            // ========================================================

            if (verification.Status == "Verified")
            {
                TempData["Success"] =
                    "Laboratory result verified successfully.";
            }
            else
            {
                TempData["Success"] =
                    "Laboratory result returned to the capturing technician for review.";
            }


            return RedirectToAction(nameof(Index));
        }
    }
}