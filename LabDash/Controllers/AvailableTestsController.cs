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
    public class AvailableTestsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public AvailableTestsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //==========================================================
        // AVAILABLE TESTS
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t =>
                    t.Status == "Submitted" &&
                    t.TestRequest.Status == "Samples Received" &&
                    _context.TechnicianTestTypes.Any(a =>
                        a.TechnicianId == technician.Id &&
                        a.TestTypeId == t.TestTypeId))
                .OrderBy(t => t.RequestId)
                .ToListAsync();

            return View(tests);
        }

        //==========================================================
        // START TEST
        //==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.Status != "Submitted")
            {
                TempData["Error"] = "This test cannot be started.";
                return RedirectToAction(nameof(Index));
            }

            //------------------------------------------------------
            // Check Consumable Stock
            //------------------------------------------------------

            var consumables = await _context.TestTypeConsumables
                .Include(c => c.Consumable)
                .Where(c => c.TestTypeId == item.TestTypeId)
                .ToListAsync();

            foreach (var consumable in consumables)
            {
                if (consumable.Consumable.StockLevel < consumable.QuantityRequired)
                {
                    TempData["Error"] =
                        $"Insufficient stock for {consumable.Consumable.Name}. " +
                        $"Required: {consumable.QuantityRequired}, " +
                        $"Available: {consumable.Consumable.StockLevel}.";

                    return RedirectToAction(nameof(Index));
                }
            }

            //------------------------------------------------------
            // Deduct Consumables
            //------------------------------------------------------

            foreach (var consumable in consumables)
            {
                consumable.Consumable.StockLevel -= consumable.QuantityRequired;
                consumable.Consumable.UpdatedAt = DateTime.Now;
            }

            //------------------------------------------------------
            // Assign Technician
            //------------------------------------------------------

            item.AssignedTechnicianId = technician.Id;
            item.StartDateTime = DateTime.Now;
            item.Status = "In Progress";

            item.TestRequest.Status = "In Progress";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test started successfully.";

            return RedirectToAction(nameof(InProgress));
        }

        //==========================================================
        // TESTS CURRENTLY IN PROGRESS
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> InProgress()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t =>
                    t.AssignedTechnicianId == technician.Id &&
                    t.Status == "In Progress")
                .OrderBy(t => t.StartDateTime)
                .ToListAsync();

            return View(tests);
        }

        //==========================================================
        // COMPLETED TESTS
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> Completed()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t =>
                    t.AssignedTechnicianId == technician.Id &&
                    (t.Status == "Completed" ||
                     t.Status == "Verified" ||
                     t.Status == "To Be Reviewed"))
                .OrderByDescending(t => t.CompletionDateTime)
                .ToListAsync();

            return View(tests);
        }

        //==========================================================
        // TEST HISTORY
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> TestHistory()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var history = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t => t.AssignedTechnicianId == technician.Id)
                .OrderByDescending(t => t.CompletionDateTime ?? t.StartDateTime)
                .ToListAsync();

            return View(history);
        }
    }
}