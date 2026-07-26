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

            var tests = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                .Where(t => t.AssignedTechnicianId == technician.Id
                         && t.Status == "In Progress")
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

            return View(new TestResult
            {
                TestRequestItemId = item.TestRequestItemId,
                ReferenceRange = referenceRangeDisplay
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

            if (!ModelState.IsValid)
            {
                ViewBag.TestItem = item;
                ViewBag.Patient = item.TestRequest.Patient;

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

            // Always store the range that was actually in effect at capture time,
            // regardless of what (if anything) was rendered in the form.
            if (item.TestType.ReferenceRangeLow.HasValue && item.TestType.ReferenceRangeHigh.HasValue)
            {
                result.ReferenceRange =
                    $"{item.TestType.ReferenceRangeLow.Value} - {item.TestType.ReferenceRangeHigh.Value}";
            }

            // Save Result
            result.DateCaptured = DateTime.Now;
            result.CapturedByTechnicianId = technician.Id;
            result.Status = "Completed";

            _context.TestResults.Add(result);

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