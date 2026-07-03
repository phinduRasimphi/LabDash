using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class ReportsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public ReportsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //--------------------------------------------------------
        // Report Page
        //--------------------------------------------------------
        [HttpGet]
        public IActionResult Index()
        {
            var model = new TechnicianReportViewModel
            {
                FromDate = DateTime.Today.AddDays(-30),
                ToDate = DateTime.Today
            };

            return View(model);
        }

        //--------------------------------------------------------
        // Generate Report
        //--------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(TechnicianReportViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            if (model.FromDate > model.ToDate)
            {
                ModelState.AddModelError("", "The From Date cannot be later than the To Date.");
                return View("Index", model);
            }

            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var completedTests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                .Where(x =>
                    x.Status == "Completed" &&
                    x.AssignedTechnicianId == technician.Id &&
                    x.CompletionDateTime >= model.FromDate &&
                    x.CompletionDateTime <= model.ToDate)
                .OrderBy(x => x.TestType.Category)
                .ThenBy(x => x.CompletionDateTime)
                .ToListAsync();

            if (!completedTests.Any())
            {
                TempData["Error"] = "No completed tests were found for the selected date range.";
                return View("Index", model);
            }

            // We will replace this with PDF generation
            return View("ReportResults", completedTests);
        }
    }
}