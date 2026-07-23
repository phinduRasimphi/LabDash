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
                .Where(t =>
                    t.AssignedTechnicianId == technician.Id &&
                   t.Status == "InProgress;")
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
            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == item.TestRequest.PatientId);

            ViewBag.TestItem = item;
            ViewBag.Patient = patient;

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
            if (!ModelState.IsValid)
            {
                var itemReload = await _context.TestRequestItems
                    .Include(t => t.TestType)
                    .Include(t => t.TestRequest)
                    .FirstOrDefaultAsync(t => t.TestRequestItemId == result.TestRequestItemId);

                ViewBag.TestItem = itemReload;

                ViewBag.Patient = await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.PatientID == itemReload.TestRequest.PatientId);

                return View(result);
            }

            var technician = await _userManager.GetUserAsync(User);

            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == result.TestRequestItemId);

            if (item == null)
                return NotFound();

            result.DateCaptured = DateTime.Now;
            result.CapturedByTechnicianId = technician.Id;

            _context.TestResults.Add(result);

            // Update Test Item
            item.Status = "Completed"; 
            item.CompletionDateTime = DateTime.Now;

            // Check if every test for the request is completed
            bool allCompleted = await _context.TestRequestItems
                .Where(x => x.RequestId == item.RequestId)
                .AllAsync(x => x.Status == "Completed");

            if (allCompleted)
            {
                item.TestRequest.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test result captured successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}