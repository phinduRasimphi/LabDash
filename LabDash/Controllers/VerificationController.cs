using LabDash.Models;
using LabDash.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class VerificationController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public VerificationController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //---------------------------------------------------------
        // Verification Queue
        //---------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var queue = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(t => t.TestRequest)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(t => t.TestType)
                .Include(r => r.CapturedByTechnician)
                .Where(r =>
                    r.Status == "Completed" &&
                    r.CapturedByTechnicianId != technician.Id &&
                    r.VerifiedByTechnicianId == null)
                .OrderBy(r => r.DateCaptured)
                .ToListAsync();

            return View(queue);
        }

        //---------------------------------------------------------
        // Display Verification Page
        //---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Verify(int id)
        {
            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .Include(t => t.AssignedTechnician)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.Status != "Completed")
            {
                TempData["Error"] = "This test is not awaiting verification.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            if (result == null)
            {
                TempData["Error"] = "No captured result exists for this test.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest.Patient;
            ViewBag.Result = result;

            return View(new TestVerification
            {
                TestRequestItemId = id,
                Status = "Verified"
            });
        }

        //---------------------------------------------------------
        // Verify Test
        //---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(TestVerification verification)
        {
            if (!ModelState.IsValid)
                return View(verification);

            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            //---------------------------------------------------------
            // Get test item
            //---------------------------------------------------------
            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == verification.TestRequestItemId);

            if (item == null)
                return NotFound();

            //---------------------------------------------------------
            // Must still be completed
            //---------------------------------------------------------
            if (item.Status != "Completed")
            {
                TempData["Error"] =
                    "This test is no longer awaiting verification.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------------
            // Get captured result
            //---------------------------------------------------------
            var result = await _context.TestResults
                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == verification.TestRequestItemId);

            if (result == null)
            {
                TempData["Error"] =
                    "No captured result exists for this test.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------------
            // Rule 1: Cannot verify own result
            //---------------------------------------------------------
            if (result.CapturedByTechnicianId == technician.Id)
            {
                TempData["Error"] =
                    "You cannot verify a result that you captured yourself.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------------
            // Rule 2: Must be assigned to this test type
            //---------------------------------------------------------
            bool assigned = await _context.TechnicianAssignments
                .AnyAsync(x =>
                    x.TechnicianId == technician.Id &&
                    x.TestTypeId == item.TestTypeId);

            if (!assigned)
            {
                TempData["Error"] =
                    "You are not authorised to verify this test type.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------------
            // Prevent double verification
            //---------------------------------------------------------
            if (!string.IsNullOrEmpty(result.VerifiedByTechnicianId))
            {
                TempData["Error"] =
                    "This result has already been verified.";

                return RedirectToAction(nameof(Index));
            }

            //---------------------------------------------------------
            // Save verification record
            //---------------------------------------------------------
            verification.VerifiedByTechnicianId = technician.Id;
            verification.VerificationDate = DateTime.Now;

            _context.TestVerifications.Add(verification);

            //---------------------------------------------------------
            // Update TestResult
            //---------------------------------------------------------
            result.VerifiedByTechnicianId = technician.Id;
            result.VerificationDate = DateTime.Now;
            result.VerificationNote = verification.VerificationNotes;
            result.Status = verification.Status;

            //---------------------------------------------------------
            // Update TestRequestItem
            //---------------------------------------------------------
            item.Status = verification.Status;

            //---------------------------------------------------------
            // If all tests are verified, verify request
            //---------------------------------------------------------
            bool allVerified = await _context.TestRequestItems
                .Where(x => x.RequestId == item.RequestId)
                .AllAsync(x => x.Status == "Verified");

            if (allVerified)
            {
                item.TestRequest.Status = "Verified";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                verification.Status == "Verified"
                    ? "Result verified successfully."
                    : "Result returned for review.";

            return RedirectToAction(nameof(Index));
        }
    }
}