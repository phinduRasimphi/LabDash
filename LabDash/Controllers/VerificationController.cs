using LabDash.Areas.Identity.Data;
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
            if (technician == null) return Challenge();

            var results = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(req => req.Patient)
                .Include(r => r.CapturedByTechnician)
                .Where(r =>
                    r.Status == TestStatus.Completed &&
                    r.VerifiedByTechnicianId == null &&
                    r.TestRequestItem != null &&
                    r.TestRequestItem.Status == TestStatus.Completed &&
                    r.CapturedByTechnicianId != technician.Id)
                .OrderBy(r => r.DateCaptured)
                .ToListAsync();

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
                .FirstOrDefaultAsync(i => i.TestRequestItemId == id);

            if (item == null) return NotFound();

            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .Include(r => r.VerifiedByTechnician)
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            var history = await _context.TestVerifications
                .Include(v => v.VerifiedByTechnician)
                .Where(v => v.TestRequestItemId == id)
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
            if (technician == null) return Challenge();

            var item = await _context.TestRequestItems
                .Include(i => i.TestType)
                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)
                .Include(i => i.AssignedTechnician)
                .FirstOrDefaultAsync(i => i.TestRequestItemId == id);

            if (item == null) return NotFound();

            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            if (result == null)
            {
                TempData["Error"] = "No laboratory result exists for this test.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Status != TestStatus.Completed)
            {
                TempData["Error"] = "This result is not waiting for verification.";
                return RedirectToAction(nameof(Index));
            }

            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] = "You cannot verify a result that you captured yourself.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(result.VerifiedByTechnicianId))
            {
                TempData["Error"] = "This result has already been verified.";
                return RedirectToAction(nameof(Index));
            }

            var reviewReturnCount = await _context.TestVerifications
                .CountAsync(v =>
                    v.TestRequestItemId == id &&
                    v.Status == TestStatus.ToBeRevised);


            // ========================================================
            // RETURNED-FOR-REVIEW HISTORY
            // Every time this test was returned, newest first.
            // Each record holds the reason and a snapshot of the
            // result as it was when it was returned.
            // ========================================================

            var returnHistory =
                await _context.TestVerifications

                    .Include(v => v.VerifiedByTechnician)

                    .Where(v =>
                        v.TestRequestItemId == id &&
                        v.Status == "To Be Reviewed"
                    )

                    .OrderByDescending(v => v.VerificationDate)

                    .ToListAsync();


            // Technician names, keyed by technician id (as a string)
            var verifierNames = returnHistory

                .Where(v =>
                    !string.IsNullOrEmpty(v.VerifiedByTechnicianId) &&
                    v.VerifiedByTechnician != null)

                .GroupBy(v => v.VerifiedByTechnicianId!)

                .ToDictionary(
                    g => g.Key,
                    g => g.First().VerifiedByTechnician!.FullName
                );



            // ========================================================
            // SEND DATA TO VIEW
            // ========================================================

            ViewBag.TestItem =
                item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            ViewBag.Result =
                result;

            ViewBag.ReviewReturnCount =
                returnHistory.Count;

            ViewBag.ReturnHistory =
                returnHistory;

            ViewBag.VerifierNames =
                verifierNames;


            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.Result = result;
            ViewBag.ReviewReturnCount = reviewReturnCount;

            return View(new TestVerification
            {
                TestRequestItemId = item.TestRequestItemId,
                Status = TestStatus.Verified
            });
        }

        // ============================================================
        // SAVE VERIFICATION
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(TestVerification verification)
        {
            var technician = await _userManager.GetUserAsync(User);
            if (technician == null) return Challenge();

            if (verification.TestRequestItemId <= 0)
            {
                TempData["Error"] = "Invalid test request item.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.TestRequestItems
                .Include(i => i.TestType)
                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(i => i.TestRequestItemId == verification.TestRequestItemId);

            if (item == null) return NotFound();

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == verification.TestRequestItemId);

            if (result == null)
            {
                TempData["Error"] = "Laboratory result could not be found.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Status != TestStatus.Completed)
            {
                TempData["Error"] = "This result is no longer waiting for verification.";
                return RedirectToAction(nameof(Index));
            }

            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] = "You cannot verify your own laboratory result.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(result.VerifiedByTechnicianId))
            {
                TempData["Error"] = "This result has already been verified.";
                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // VALIDATE DECISION
            // --------------------------------------------------------
            if (verification.Status != TestStatus.Verified &&
                verification.Status != TestStatus.ToBeRevised)
            {
                TempData["Error"] = "Please select either Verify Result or Return For Review.";
                return RedirectToAction(nameof(Verify), new { id = verification.TestRequestItemId });
            }

            // --------------------------------------------------------
            // REVIEW NOTE REQUIRED WHEN RETURNING
            // --------------------------------------------------------
            if (verification.Status == TestStatus.ToBeRevised &&
                string.IsNullOrWhiteSpace(verification.VerificationNotes))
            {
                TempData["Error"] = "Please provide a reason when returning a result for review.";
                return RedirectToAction(nameof(Verify), new { id = verification.TestRequestItemId });
            }

            // --------------------------------------------------------
            // HISTORY RECORD
            // --------------------------------------------------------
            var history = new TestVerification
            {
                TestRequestItemId = item.TestRequestItemId,
                VerifiedByTechnicianId = technician.Id,
                VerificationDate = DateTime.Now,
                Status = verification.Status,
                VerificationNotes = verification.VerificationNotes
            };


            // --------------------------------------------------------
            // SNAPSHOT THE RESULT AS IT IS NOW (before the technician
            // corrects it), so the Verify page can later show
            // "before review" vs "after review".
            // --------------------------------------------------------

            if (verification.Status == "To Be Reviewed")
            {
                history.PreviousResultValue = result.ResultValue;
                history.PreviousComments = result.Comments;
                history.PreviousIsAbnormal = result.IsAbnormal;
            }


            _context.TestVerifications.Add(history);

            // --------------------------------------------------------
            // APPLY DECISION
            // --------------------------------------------------------
            if (verification.Status == TestStatus.Verified)
            {
                result.Status = TestStatus.Verified;
                result.VerifiedByTechnicianId = technician.Id;
                result.VerificationDate = DateTime.Now;
                result.VerificationNote = verification.VerificationNotes;

                item.Status = TestStatus.Verified;
            }
            else
            {
                result.Status = TestStatus.ToBeRevised;
                result.VerifiedByTechnicianId = null;
                result.VerificationDate = null;
                result.VerificationNote = verification.VerificationNotes;

                item.Status = TestStatus.ToBeRevised;
                item.ReviewReturnCount++;
                item.AssignedTechnicianId = result.CapturedByTechnicianId;
            }

            // --------------------------------------------------------
            // IS THE WHOLE REQUEST NOW VERIFIED?
            // --------------------------------------------------------
            bool allVerified = false;

            if (verification.Status == TestStatus.Verified)
            {
                allVerified = !await _context.TestRequestItems
                    .AnyAsync(x =>
                        x.RequestId == item.RequestId &&
                        x.TestRequestItemId != item.TestRequestItemId &&
                        x.Status != TestStatus.Verified);

                if (allVerified)
                {
                    // Request-level status is "Completed" once EVERY item is Verified.
                    item.TestRequest.Status = TestRequestStatus.Completed;
                }
            }

            // --------------------------------------------------------
            // SAVE
            // --------------------------------------------------------
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

            // --------------------------------------------------------
            // EMAIL THE DOCTOR — once, when the whole request is verified
            // --------------------------------------------------------
            if (allVerified && item.TestRequest.ResultsEmailedAt == null)
            {
                var sent = await _notificationService
                    .SendVerifiedResultsAsync(item.RequestId);

                if (sent)
                {
                    item.TestRequest.ResultsEmailedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["Warning"] =
                        "Result verified, but the email to the doctor could not be sent. " +
                        "The doctor can still view the results in the patient folder.";
                }
            }

            TempData["Success"] = verification.Status == TestStatus.Verified
                ? "Laboratory result verified successfully."
                : "Laboratory result returned to the capturing technician for review.";

            return RedirectToAction(nameof(Index));
        }
    }
}