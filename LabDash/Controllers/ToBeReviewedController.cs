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

        //-------------------------------------------------------
        // Tests returned for review
        //-------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t =>
                    t.Status == "To Be Reviewed" &&
                    t.AssignedTechnicianId == technician.Id)
                .OrderByDescending(t => t.CompletionDateTime)
                .ToListAsync();

            return View(tests);
        }

        //-------------------------------------------------------
        // Display returned test
        //-------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Include(t => t.AssignedTechnician)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == id);

            var verification = await _context.TestVerifications
                .Where(v => v.TestRequestItemId == id)
                .OrderByDescending(v => v.VerificationDate)
                .FirstOrDefaultAsync();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.PatientID == item.TestRequest.PatientId);

            ViewBag.TestItem = item;
            ViewBag.Patient = patient;
            ViewBag.Verification = verification;

            return View(result);
        }

        //-------------------------------------------------------
        // Save corrected result
        //-------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(TestResult model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r =>
                    r.ResultId == model.ResultId);

            if (result == null)
                return NotFound();

            // Update result
            result.ResultValue = model.ResultValue;
            result.Units = model.Units;
            result.ReferenceRange = model.ReferenceRange;
            result.Comments = model.Comments;
            result.DateCaptured = DateTime.Now;

            // Return test to Completed
            var item = await _context.TestRequestItems
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == result.TestRequestItemId);

            if (item != null)
            {
                item.Status = "Completed";
                item.CompletionDateTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Result updated and sent back for verification.";

            return RedirectToAction(nameof(Index));
        }
    }
}
