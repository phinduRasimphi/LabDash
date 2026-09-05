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


        // ============================================================
        // TESTS RETURNED FOR REVIEW
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // FIND TESTS RETURNED TO THIS TECHNICIAN
            // --------------------------------------------------------
            var tests = await _context.TestRequestItems

                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(t => t.TestType)

                .Include(t => t.AssignedTechnician)

                .Where(t =>
                    t.Status == "To Be Reviewed" &&
                    t.AssignedTechnicianId == technician.Id
                )

                .OrderByDescending(t => t.CompletionDateTime)

                .ToListAsync();


            return View(tests);
        }


        // ============================================================
        // DISPLAY RETURNED TEST
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // LOAD TEST
            // --------------------------------------------------------
            var item = await _context.TestRequestItems

                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(t => t.TestType)

                .Include(t => t.AssignedTechnician)

                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == id);


            if (item == null)
                return NotFound();


            // --------------------------------------------------------
            // MAKE SURE THIS TEST BELONGS TO CURRENT TECHNICIAN
            // --------------------------------------------------------
            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "This test is not assigned to you.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // TEST MUST BE RETURNED FOR REVIEW
            // --------------------------------------------------------
            if (item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer waiting for review.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------
            var result = await _context.TestResults

                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == id);


            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result was found for this test.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // LOAD MOST RECENT VERIFICATION
            // --------------------------------------------------------
            var verification = await _context.TestVerifications

                .Include(v => v.VerifiedByTechnician)

                .Where(v =>
                    v.TestRequestItemId == id &&
                    v.Status == "To Be Reviewed"
                )

                .OrderByDescending(v => v.VerificationDate)

                .FirstOrDefaultAsync();


            // --------------------------------------------------------
            // SEND DATA TO VIEW
            // --------------------------------------------------------
            ViewBag.TestItem = item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            ViewBag.Verification =
                verification;


            return View(result);
        }


        // ============================================================
        // SAVE CORRECTED RESULT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(TestResult model)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            // --------------------------------------------------------
            // VALIDATE MODEL
            // --------------------------------------------------------
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please correct the errors before submitting.";

                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = model.TestRequestItemId
                    });
            }


            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------
            var result = await _context.TestResults

                .FirstOrDefaultAsync(r =>
                    r.ResultId == model.ResultId);


            if (result == null)
                return NotFound();


            // --------------------------------------------------------
            // LOAD TEST ITEM
            // --------------------------------------------------------
            var item = await _context.TestRequestItems

                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId ==
                    result.TestRequestItemId);


            if (item == null)
                return NotFound();


            // --------------------------------------------------------
            // SECURITY CHECK
            // --------------------------------------------------------
            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to this test.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // CHECK STATUS
            // --------------------------------------------------------
            if (item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer waiting for review.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // UPDATE RESULT
            // ========================================================

            result.ResultValue =
                model.ResultValue;

            result.Units =
                model.Units;

            result.ReferenceRange =
                model.ReferenceRange;

            result.Comments =
                model.Comments;

            result.DateCaptured =
                DateTime.Now;


            // --------------------------------------------------------
            // REMOVE OLD VERIFICATION ASSIGNMENT
            // --------------------------------------------------------
            result.VerifiedByTechnicianId = null;

            result.VerificationDate = null;

            result.VerificationNote = null;


            // ========================================================
            // SEND BACK TO VERIFICATION QUEUE
            // ========================================================

            result.Status = "Completed";

            item.Status = "Completed";


            // Keep the original technician assigned
            // so the corrected result can be verified
            // by another technician.


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Result updated successfully and sent back for verification.";


            return RedirectToAction(nameof(Index));
        }
    }
}
