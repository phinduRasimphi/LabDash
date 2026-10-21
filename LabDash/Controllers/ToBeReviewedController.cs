using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

            // Latest verification record (used for the "Latest Verification Feedback" card)
            var verification = await _context.TestVerifications
                .Include(x => x.VerifiedByTechnician)
                .Where(x =>
                    x.TestRequestItemId == id)
                .OrderByDescending(x => x.VerificationDate)
                .FirstOrDefaultAsync();

            // Every time this test was returned for review, newest first.
            // Each row holds the reason plus a snapshot of the result
            // as it was when it was returned.
            var returnHistory = await _context.TestVerifications
                .Include(x => x.VerifiedByTechnician)
                .Where(x =>
                    x.TestRequestItemId == id &&
                    x.Status == "To Be Reviewed")
                .OrderByDescending(x => x.VerificationDate)
                .ToListAsync();

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.Verification = verification;
            ViewBag.ReturnHistory = returnHistory;

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
            // SERVER-SIDE VALIDATION OF THE NEW VALUE
            // (the browser check can be bypassed)
            // Redirecting to the GET action reloads the result
            // history automatically.
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(model.ResultValue) ||
                !Regex.IsMatch(model.ResultValue, @"^[0-9]+([.][0-9]+)?$"))
            {
                TempData["Error"] =
                    "Result value must be a number (digits and one decimal point only).";

                return RedirectToAction(
                    nameof(Review),
                    new { id = item.TestRequestItemId });
            }

            // -----------------------------------------------------
            // UPDATE THE CORRECTED RESULT
            // Units and ReferenceRange are read-only on the form and
            // are not posted, so they are intentionally left unchanged.
            // The previous attempt is already preserved in the
            // TestVerification snapshot created when it was returned.
            // -----------------------------------------------------
            result.ResultValue = model.ResultValue;
            result.Comments = model.Comments;
            result.DateCaptured = DateTime.Now;

            // -----------------------------------------------------
            // SEND THE CORRECTED RESULT BACK TO VERIFICATION
            // -----------------------------------------------------
            result.Status = "Completed";

            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            // Do not increment ReviewReturnCount here.
            // This action corrects a result that was already returned.
            // The counter must increase when a verifier returns it again.

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Result corrected successfully and returned to the verification queue.";

            // Send technician to the Verification queue.
            return RedirectToAction(
                "Index",
                "Verification");
        }
    }
}