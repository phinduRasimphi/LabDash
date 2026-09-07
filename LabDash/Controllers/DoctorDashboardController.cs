using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        // This is the CONSTRUCTOR that stops the error!
        public DoctorDashboardController(LabDbContext context, UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> DoctorIndex()
        {
            var doctor = await _userManager.GetUserAsync(User);

            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalRequests = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctor.Id).CountAsync();
            ViewBag.PendingRequests = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctor.Id && (r.Status == "Submitted" || r.Status == "Samples Received")).CountAsync();
            ViewBag.CompletedRequests = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctor.Id && (r.Status == "Completed" || r.Status == "Released by doctor")).CountAsync();
            ViewBag.AbnormalResults = await _context.TestResults
                .Where(r => r.IsAbnormal && r.TestRequestItem.TestRequest.RequestingDoctorId == doctor.Id).CountAsync();

            ViewBag.RecentRequests = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems).ThenInclude(i => i.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToListAsync();

            return View("~/Views/Dashboard/DoctorIndex.cshtml");
        }
    }
}