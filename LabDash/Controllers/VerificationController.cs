using LabDash.Models;
using LabDash.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.Services;

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

        //==========================================================
        // FULL REVIEW / VERIFICATION HISTORY
        //==========================================================
        // Read-only audit trail for a single TestRequestItem. Every
        // verification decision (approve or return, with its note) is
        // already persisted in TestVerifications by the Verify action
        // below — this simply surfaces it. Available to any lab
        // technician; it's an audit view, not a decision-making one,
        // so the "cannot review own result" / assignment rules that
        // gate the Verify action don't apply here.
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            var currentResult = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            var history = await _context.TestVerifications
                .Include(v => v.VerifiedByTechnician)
                .Where(v => v.TestRequestItemId == id)
                .OrderBy(v => v.VerificationDate)
                .ToListAsync();

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest.Patient;
            ViewBag.CurrentResult = currentResult;

            return View(history);
        }

        //==========================================================
        // VERIFICATION QUEUE
        //==========================================================
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var queue = await _context.TestResults

                .Include(r => r.TestRequestItem)
                    .ThenInclude(t => t.TestType)

                .Include(r => r.TestRequestItem)
                    .ThenInclude(t => t.TestRequest)
                        .ThenInclude(r => r.Patient)

                .Include(r => r.CapturedByTechnician)

                .Where(r =>
                    r.Status == "Completed" &&
                    r.VerifiedByTechnicianId == null &&
                    r.CapturedByTechnicianId != technician.Id)

                .OrderBy(r => r.DateCaptured)

                .ToListAsync();

            // Only show results for test types assigned
            // to this technician

            var assignedTestTypes = await _context.TechnicianAssignments

                .Where(a => a.TechnicianId == technician.Id)

                .Select(a => a.TestTypeId)

                .ToListAsync();

            queue = queue

                .Where(r =>
                    assignedTestTypes.Contains(
                        r.TestRequestItem.TestTypeId))

                .ToList();

            return View(queue);
        }

        //==========================================================
        // DISPLAY VERIFICATION PAGE
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> Verify(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems

                .Include(t => t.TestType)

                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(t => t.AssignedTechnician)

                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            var result = await _context.TestResults

                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == id &&
                    r.VerifiedByTechnicianId == null);

            if (result == null)
            {
                TempData["Error"] =
                    "This laboratory result has already been verified.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------
            // Rule 1
            // Cannot verify own result
            //---------------------------------------------------

            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] =
                    "You cannot verify a laboratory result that you captured yourself.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------
            // Rule 2
            // Must be assigned to the Test Type
            //---------------------------------------------------

            bool assigned = await _context.TechnicianAssignments

                .AnyAsync(a =>
                    a.TechnicianId == technician.Id &&
                    a.TestTypeId == item.TestTypeId);

            if (!assigned)
            {
                TempData["Error"] =
                    "You are not authorised to verify this test type.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------
            // Test must still be awaiting verification
            //---------------------------------------------------

            if (item.Status != "Completed")
            {
                TempData["Error"] =
                    "This laboratory result is no longer awaiting verification.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest.Patient;
            ViewBag.Result = result;

            return View(new TestVerification
            {
                TestRequestItemId = item.TestRequestItemId,
                Status = "Verified"
            });
        }

        //==========================================================
        // VERIFY RESULT
        //==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(TestVerification verification)
        {
            //----------------------------------------------------
            // A note is mandatory whenever a result is being
            // returned for review — approvals may still include
            // an optional comment, but a return must explain why.
            // This runs before the ModelState check below so it
            // feeds into the same reload-and-redisplay path.
            //----------------------------------------------------
            if (verification.Status == "To Be Reviewed"
                && string.IsNullOrWhiteSpace(verification.VerificationNotes))
            {
                ModelState.AddModelError(
                    nameof(verification.VerificationNotes),
                    "Please provide a note explaining why this result is being returned for review.");
            }

            if (!ModelState.IsValid)
            {
                var itemReload = await _context.TestRequestItems
                    .Include(t => t.TestType)
                    .Include(t => t.TestRequest)
                        .ThenInclude(r => r.Patient)
                    .FirstOrDefaultAsync(t =>
                        t.TestRequestItemId == verification.TestRequestItemId);

                if (itemReload != null)
                {
                    ViewBag.TestItem = itemReload;
                    ViewBag.Patient = itemReload.TestRequest.Patient;

                    ViewBag.Result = await _context.TestResults
                        .FirstOrDefaultAsync(r =>
                            r.TestRequestItemId == verification.TestRequestItemId);
                }

                return View(verification);
            }

            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            //----------------------------------------------------
            // Get Test Item
            //----------------------------------------------------
            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == verification.TestRequestItemId);

            if (item == null)
                return NotFound();

            //----------------------------------------------------
            // Get Result
            //----------------------------------------------------
            var result = await _context.TestResults
                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == verification.TestRequestItemId);

            if (result == null)
            {
                TempData["Error"] = "Laboratory result not found.";
                return RedirectToAction(nameof(Index));
            }

            //----------------------------------------------------
            // Rule 1
            // Cannot verify own result
            //----------------------------------------------------
            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] =
                    "You cannot verify a laboratory result that you captured.";

                return RedirectToAction(nameof(Index));
            }

            //----------------------------------------------------
            // Rule 2
            // Technician must be assigned
            //----------------------------------------------------
            bool assigned = await _context.TechnicianAssignments
                .AnyAsync(x =>
                    x.TechnicianId == technician.Id &&
                    x.TestTypeId == item.TestTypeId);

            if (!assigned)
            {
                TempData["Error"] =
                    "You are not authorised to verify this laboratory test.";

                return RedirectToAction(nameof(Index));
            }

            //----------------------------------------------------
            // Test must still be awaiting verification.
            // Without this check, a stale or replayed form post could
            // verify an item whose status has since moved on (e.g. it
            // was already verified, or reassigned for rework).
            //----------------------------------------------------
            if (item.Status != "Completed")
            {
                TempData["Error"] =
                    "This laboratory result is no longer awaiting verification.";

                return RedirectToAction(nameof(Index));
            }

            //----------------------------------------------------
            // Prevent double verification
            //----------------------------------------------------
            if (!string.IsNullOrEmpty(result.VerifiedByTechnicianId))
            {
                TempData["Error"] =
                    "This laboratory result has already been verified.";

                return RedirectToAction(nameof(Index));
            }

            //----------------------------------------------------
            // Save Verification History
            //----------------------------------------------------
            verification.VerifiedByTechnicianId = technician.Id;
            verification.VerificationDate = DateTime.Now;

            _context.TestVerifications.Add(verification);

            //----------------------------------------------------
            // APPROVED
            //----------------------------------------------------
            if (verification.Status == "Verified")
            {
                result.Status = "Verified";
                result.VerifiedByTechnicianId = technician.Id;
                result.VerificationDate = DateTime.Now;
                result.VerificationNote = verification.VerificationNotes;

                item.Status = "Verified";
            }

            //----------------------------------------------------
            // RETURN FOR REVIEW
            //----------------------------------------------------
            else
            {
                result.Status = "To Be Reviewed";
                result.VerifiedByTechnicianId = technician.Id;
                result.VerificationDate = DateTime.Now;
                result.VerificationNote = verification.VerificationNotes;

                item.Status = "To Be Reviewed";

                // Return to original technician
                item.AssignedTechnicianId = result.CapturedByTechnicianId;
            }

            //----------------------------------------------------
            // Persist the result/item status changes first. The
            // "is everything verified" check below queries the
            // database directly, so it must run after this commit —
            // otherwise this item's own status change isn't visible
            // yet and the check can never succeed on the very last
            // item of a request (exactly the case that matters).
            //----------------------------------------------------
            await _context.SaveChangesAsync();

            //----------------------------------------------------
            // Check whether entire request is verified
            //----------------------------------------------------
            bool allVerified = await _context.TestRequestItems
                .Where(x => x.RequestId == item.RequestId)
                .AllAsync(x => x.Status == "Verified");

            if (allVerified)
            {
                item.TestRequest.Status = "Verified";

                await _context.SaveChangesAsync();

                // Fired after both saves have committed. The service
                // itself swallows and logs any failure (bad doctor email,
                // SMTP outage, PDF error) — a notification problem must
                // never look like the verification itself failed, since
                // the result is already correctly saved at this point.
                await _notificationService.SendVerifiedResultsAsync(item.RequestId);
            }

            TempData["Success"] =
                verification.Status == "Verified"
                ? "Laboratory result verified successfully."
                : "Laboratory result returned to the original technician for review.";

            return RedirectToAction(nameof(Index));
        }
    }
}