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

            return View(new TestResult
            {
                TestRequestItemId = item.TestRequestItemId
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

            TempData["Success"] = "Test result captured successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}