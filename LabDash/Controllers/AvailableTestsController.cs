using LabDash.Areas.Identity.Data;
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

        // Display tests available for the logged-in technician
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
                .ToListAsync();

            return View(tests);
        }

        // Technician starts a test
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

            // Get all consumables required for this test
            var consumables = await _context.TestTypeConsumables
                .Include(x => x.Consumable)
                .Where(x => x.TestTypeId == item.TestTypeId)
                .ToListAsync();

            // ============================================
            // CHECK THAT THERE IS ENOUGH STOCK
            // ============================================
            foreach (var c in consumables)
            {
                if (c.Consumable.StockLevel < c.QuantityRequired)
                {
                    TempData["Error"] =
                        $"Cannot start the test. Not enough stock for '{c.Consumable.Name}'. " +
                        $"Available: {c.Consumable.StockLevel}, Required: {c.QuantityRequired}.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // ============================================
            // DEDUCT STOCK
            // ============================================
            foreach (var c in consumables)
            {
                c.Consumable.StockLevel -= c.QuantityRequired;
                c.Consumable.UpdatedAt = DateTime.Now;
            }

            // ============================================
            // ASSIGN TECHNICIAN
            // ============================================
            item.AssignedTechnicianId = technician.Id;
            item.StartDateTime = DateTime.Now;
            item.Status = "In Progress";

            var expectedCompletion =
    item.StartDateTime.Value.AddHours(item.TestType.TurnaroundTimeHours);

            // If this is the first test started for the request,
            // update the request status as well.
            bool firstTest = !_context.TestRequestItems.Any(x =>
                x.RequestId == item.RequestId &&
                x.Status == "In Progress");

            if (firstTest)
            {
                item.TestRequest.Status = "In Progress";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test started successfully. Consumables have been deducted from stock.";

            return RedirectToAction(nameof(Index));
        }
    }
}