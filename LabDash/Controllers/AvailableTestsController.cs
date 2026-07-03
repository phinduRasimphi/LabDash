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

        [HttpPost]
        public async Task<IActionResult> StartTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            item.AssignedTechnicianId = technician.Id;
            item.StartDateTime = DateTime.Now;
            item.Status = "In Progress";

            bool firstTest =
                !_context.TestRequestItems.Any(x =>
                    x.RequestId == item.RequestId &&
                    x.Status == "In Progress");

            if (firstTest)
            {
                item.TestRequest.Status = "In Progress";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}