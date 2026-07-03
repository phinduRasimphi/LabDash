using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class DashboardController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public DashboardController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // Tests selected by this technician
            ViewBag.SelectedTests = await _context.TestRequestItems
                .CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "In Progress");

            // Waiting to be selected
            ViewBag.WaitingSelection = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .CountAsync(x =>
                    x.Status == "Submitted" &&
                    x.TestRequest.Status == "Samples Received");

            // Waiting verification
            ViewBag.WaitingVerification = await _context.TestRequestItems
                .CountAsync(x =>
                    x.Status == "Completed");

            // Waiting review
            ViewBag.WaitingReview = await _context.TestRequestItems
                .CountAsync(x =>
                    x.Status == "To Be Reviewed" &&
                    x.AssignedTechnicianId == technician.Id);

            // STAT tests
            ViewBag.StatTests = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .CountAsync(x =>
                    x.TestRequest.Urgency == "STAT");

            // Overdue tests
            ViewBag.OverdueTests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .CountAsync(x =>
                    x.Status == "In Progress" &&
                    x.StartDateTime.HasValue &&
                    DateTime.Now >
                    x.StartDateTime.Value.AddHours(x.TestType.TurnaroundTimeHours));

            // Nearing turnaround (30 mins remaining)
            ViewBag.NearingDeadline = await _context.TestRequestItems
                .Include(x => x.TestType)
                .CountAsync(x =>
                    x.Status == "In Progress" &&
                    x.StartDateTime.HasValue &&
                    DateTime.Now >=
                        x.StartDateTime.Value
                            .AddHours(x.TestType.TurnaroundTimeHours)
                            .AddMinutes(-30) &&
                    DateTime.Now <=
                        x.StartDateTime.Value
                            .AddHours(x.TestType.TurnaroundTimeHours));
            ViewBag.TestQueue = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Where(t => t.AssignedTechnicianId == technician.Id)
                .OrderBy(t => t.StartDateTime)
                .ToListAsync();

            return View();
        }
    }
}