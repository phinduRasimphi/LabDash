using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class ToBeReviewedController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public ToBeReviewedController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // TESTS RETURNED FOR REVIEW
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .Where(x =>
                    x.Status == "To Be Reviewed" &&
                    x.AssignedTechnicianId == technician.Id
                )
                .OrderByDescending(x => x.CompletionDateTime)
                .ToListAsync();

            return View(tests);
        }

        // =========================================================
        // OPEN TEST FOR REVIEW
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            // Only the technician who originally captured
            // the result can review the returned test.
            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to review this test.";

                return RedirectToAction(nameof(Index));
            }

            // Make sure this is actually a returned test.
            if (item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is not currently waiting for review.";

                return RedirectToAction(nameof(Index));
            }

            var result = await _context.TestResults
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result was found for this test.";

                return RedirectToAction(nameof(Index));
            }

            var verification = await _context.TestVerifications
                .Include(x => x.VerifiedByTechnician)
                .Where(x =>
                    x.TestRequestItemId == id)
                .OrderByDescending(x => x.VerificationDate)
                .FirstOrDefaultAsync();

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.Verification = verification;

            return View(result);
        }

        // =========================================================
        // SAVE CORRECTED RESULT
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(TestResult model)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // -----------------------------------------------------
            // LOAD RESULT
            // -----------------------------------------------------
            var result = await _context.TestResults
                .FirstOrDefaultAsync(x =>
                    x.ResultId == model.ResultId);

            if (result == null)
            {
                TempData["Error"] =
                    "Laboratory result could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // LOAD TEST ITEM
            // -----------------------------------------------------
            var item = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId ==
                    result.TestRequestItemId);

            if (item == null)
            {
                TempData["Error"] =
                    "Test could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // SECURITY CHECK
            // -----------------------------------------------------
            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to this test.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // STATUS CHECK
            // -----------------------------------------------------
            if (item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is not currently waiting for review.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // UPDATE RESULT
            // -----------------------------------------------------
            result.ResultValue = model.ResultValue;
            result.Units = model.Units;
            result.ReferenceRange = model.ReferenceRange;
            result.Comments = model.Comments;

            result.DateCaptured = DateTime.Now;

            // -----------------------------------------------------
            // RESULT GOES BACK TO VERIFICATION
            // -----------------------------------------------------
            result.Status = "Completed";

            // The original technician is still the
            // AssignedTechnicianId.

            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Result corrected successfully and returned to the verification queue.";

            // IMPORTANT:
            // Send technician to Verification queue.
            return RedirectToAction(
                "Index",
                "Verification");
        }
    }
}
