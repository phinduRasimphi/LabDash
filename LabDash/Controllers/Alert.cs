using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class AlertsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public AlertsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =============================================================
        // INDEX — abnormal alerts for tests ordered by this doctor
        // Default: last 5 days. Custom: from/to date range.
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            string? urgency,
            bool onlyUnread = false)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            // ----- Default range: last 5 days -----
            DateTime today = DateTime.Today;
            DateTime effectiveFrom = fromDate?.Date ?? today.AddDays(-5);
            DateTime effectiveTo = toDate?.Date ?? today;

            // Ensure To is inclusive of the entire day
            DateTime toInclusive = effectiveTo.AddDays(1);

            // ----- Query: abnormal results for this doctor's requests -----
            var query = _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(t => t.Patient)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(t => t.RequestingDoctor)
                .Where(r => r.IsAbnormal)
                .Where(r => r.TestRequestItem != null)
                .Where(r => r.TestRequestItem.TestRequest != null)
                .Where(r => r.TestRequestItem.TestRequest.RequestingDoctorId == doctor.Id)
                .Where(r => r.DateCaptured >= effectiveFrom && r.DateCaptured < toInclusive);

            // Optional urgency filter
            if (!string.IsNullOrWhiteSpace(urgency) && urgency != "All")
            {
                query = query.Where(r => r.TestRequestItem.TestRequest.Urgency == urgency);
            }

            var alerts = await query
                .OrderByDescending(r => r.DateCaptured)
                .ToListAsync();

            // ----- Build view model -----
            var model = new AlertsViewModel
            {
                FromDate = effectiveFrom,
                ToDate = effectiveTo,
                UrgencyFilter = urgency ?? "All",
                Alerts = alerts.Select(r => new AlertRow
                {
                    ResultId = r.ResultId,
                    RequestId = r.TestRequestItem!.TestRequest!.RequestId,
                    PatientId = r.TestRequestItem.TestRequest.PatientId,
                    PatientName = r.TestRequestItem.TestRequest.Patient?.Name + " " +
                                    r.TestRequestItem.TestRequest.Patient?.Surname,
                    PatientIdNumber = r.TestRequestItem.TestRequest.Patient?.IDNumber ?? "",
                    PatientEmail = r.TestRequestItem.TestRequest.Patient?.Email ?? "",
                    TestName = r.TestRequestItem.TestType?.Name ?? "Unknown Test",
                    TestCategory = r.TestRequestItem.TestType?.Category ?? "",
                    ResultValue = r.ResultValue,
                    Units = r.Units ?? "",
                    ReferenceRange = r.ReferenceRange ?? "",
                    Comments = r.Comments ?? "",
                    DateCaptured = r.DateCaptured,
                    Urgency = r.TestRequestItem.TestRequest.Urgency,
                    RequestStatus = r.TestRequestItem.TestRequest.Status,
                    Verified = !string.IsNullOrWhiteSpace(r.VerifiedByTechnicianId)
                }).ToList()
            };

            // Counts
            ViewBag.TotalAlerts = model.Alerts.Count;
            ViewBag.CriticalCount = model.Alerts.Count(a =>
                a.Urgency.Equals("STAT", StringComparison.OrdinalIgnoreCase));
            ViewBag.Past5Days = model.Alerts.Count(a =>
                a.DateCaptured >= today.AddDays(-5));
            ViewBag.UniquePatients = model.Alerts
                .Select(a => a.PatientId)
                .Distinct()
                .Count();

            return View(model);
        }

        // =============================================================
        // QUICK FILTER — last 5 days
        // =============================================================
        [HttpGet]
        public IActionResult Last5Days()
        {
            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // QUICK FILTER — last 30 days
        // =============================================================
        [HttpGet]
        public IActionResult Last30Days()
        {
            return RedirectToAction(nameof(Index), new
            {
                fromDate = DateTime.Today.AddDays(-30),
                toDate = DateTime.Today
            });
        }

        // =============================================================
        // DETAILS — single result deep dive
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var result = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(t => t.Patient)
                .Include(r => r.CapturedByTechnician)
                .Include(r => r.VerifiedByTechnician)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound();

            if (result.TestRequestItem?.TestRequest?.RequestingDoctorId != doctor.Id)
                return Forbid();

            return View(result);
        }
    }
}