using LabDash.Areas.Identity.Data;
using LabDash.Enums;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class CaptureResultsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public CaptureResultsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //====================================================
        // DISPLAY TESTS ASSIGNED TO LOGGED-IN TECHNICIAN
        //====================================================
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // "In Progress"     -> first-time capture
            // "To Be Reviewed"  -> returned by a verifier, reassigned back
            //                      to this technician for resubmission
            var tests = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                .Where(t => t.AssignedTechnicianId == technician.Id
                         && (t.Status == "In Progress" || t.Status == "To Be Reviewed"))
                .OrderBy(t => t.StartDateTime)
                .ToListAsync();

            return View(tests);
        }

        //====================================================
        // DISPLAY CAPTURE RESULT PAGE
        //====================================================
        [HttpGet]
        public async Task<IActionResult> Capture(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            // Prevent technicians accessing other technicians' tests
            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // Only items still awaiting a first capture, or returned for
            // rework, are eligible — a Completed/Verified item shouldn't
            // be re-openable here.
            if (item.Status != "In Progress" && item.Status != "To Be Reviewed")
            {
                TempData["Error"] = "This test is no longer available for result capture.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest.Patient;

            // Pre-fill the reference range from the test type itself, rather than
            // relying on the technician to type it in. If the test type doesn't
            // define a numeric range (e.g. a qualitative test), this is left blank
            // and automatic abnormal-detection simply won't apply to that result.
            string? referenceRangeDisplay = null;

            if (item.TestType.ReferenceRangeLow.HasValue && item.TestType.ReferenceRangeHigh.HasValue)
            {
                referenceRangeDisplay =
                    $"{item.TestType.ReferenceRangeLow.Value} - {item.TestType.ReferenceRangeHigh.Value}";
            }

            // On a resubmission there is already a TestResult tied to this item —
            // load it so the technician sees (and can adjust) what they previously
            // entered, plus the reviewer's note explaining why it came back.
            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == item.TestRequestItemId);

            ViewBag.IsResubmission = item.Status == "To Be Reviewed";
            ViewBag.PreviousVerificationNote = existingResult?.VerificationNote;

            return View(new TestResult
            {
                TestRequestItemId = item.TestRequestItemId,
                ResultValue = existingResult?.ResultValue,
                Units = existingResult?.Units,
                ReferenceRange = referenceRangeDisplay ?? existingResult?.ReferenceRange,
                Comments = existingResult?.Comments
            });
        }

        //====================================================
        // SAVE RESULT
        //====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(TestResult result)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == result.TestRequestItemId);

            if (item == null)
                return NotFound();

            // Prevent technicians accessing other technicians' tests
            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            if (item.Status != "In Progress" && item.Status != "To Be Reviewed")
            {
                TempData["Error"] = "This test is no longer available for result capture.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TestItem = item;
                ViewBag.Patient = item.TestRequest.Patient;
                ViewBag.IsResubmission = item.Status == "To Be Reviewed";

                var existingForReload = await _context.TestResults
                    .FirstOrDefaultAsync(r => r.TestRequestItemId == result.TestRequestItemId);
                ViewBag.PreviousVerificationNote = existingForReload?.VerificationNote;

                return View(result);
            }

            // ------------------------------------------------------------------
            // AUTOMATIC ABNORMAL RESULT DETECTION (server-side, authoritative)
            // ------------------------------------------------------------------
            // The reference range always comes from the test type, never from
            // whatever the client submitted, so this can't be bypassed or
            // tampered with by disabling JavaScript or editing form values.
            result.IsAbnormal = false;

            if (item.TestType.ReferenceRangeLow.HasValue
                && item.TestType.ReferenceRangeHigh.HasValue
                && decimal.TryParse(result.ResultValue, out var numericResult))
            {
                result.IsAbnormal =
                    numericResult < item.TestType.ReferenceRangeLow.Value ||
                    numericResult > item.TestType.ReferenceRangeHigh.Value;
            }

            if (item.TestType.ReferenceRangeLow.HasValue && item.TestType.ReferenceRangeHigh.HasValue)
            {
                result.ReferenceRange =
                    $"{item.TestType.ReferenceRangeLow.Value} - {item.TestType.ReferenceRangeHigh.Value}";
            }

            // ------------------------------------------------------------------
            // FIRST CAPTURE vs RESUBMISSION
            // ------------------------------------------------------------------
            // A TestRequestItem should only ever have one TestResult tied to it.
            // On a resubmission (item.Status == "To Be Reviewed") that row
            // already exists, so it's updated in place rather than inserted
            // again — otherwise the verification queue's "unverified result
            // for this item" lookup becomes ambiguous between two rows.
            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == result.TestRequestItemId);

            if (existingResult != null)
            {
                existingResult.ResultValue = result.ResultValue;
                existingResult.Units = result.Units;
                existingResult.ReferenceRange = result.ReferenceRange;
                existingResult.Comments = result.Comments;
                existingResult.IsAbnormal = result.IsAbnormal;
                existingResult.DateCaptured = DateTime.Now;
                existingResult.CapturedByTechnicianId = technician.Id;
                existingResult.Status = "Completed";

                // Clear the prior verification outcome so this cycles back into
                // the verification queue cleanly. The earlier decision and note
                // are not lost — they remain on record in TestVerifications.
                existingResult.VerifiedByTechnicianId = null;
                existingResult.VerificationDate = null;
                existingResult.VerificationNote = null;
            }
            else
            {
                result.DateCaptured = DateTime.Now;
                result.CapturedByTechnicianId = technician.Id;
                result.Status = "Completed";

                _context.TestResults.Add(result);
            }

            // Update Test Item
            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            // Check if all tests on the request are completed
            bool allCompleted = await _context.TestRequestItems
                .Where(x => x.RequestId == item.RequestId)
                .AllAsync(x => x.Status == "Completed");

            if (allCompleted)
            {
                item.TestRequest.Status = "Completed";

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = result.IsAbnormal
                ? "Test result captured successfully. Result flagged as abnormal."
                : "Test result captured successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}