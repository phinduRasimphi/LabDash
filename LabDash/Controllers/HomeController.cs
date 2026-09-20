using System.Diagnostics;
using System.Security.Claims;
using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LabDbContext _context;

        public HomeController(ILogger<HomeController> logger, LabDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var patient = !string.IsNullOrEmpty(userId)
                    ? await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId)
                    : null;

                if (patient == null)
                    _logger.LogWarning("Dashboard: no Patient record linked to user {UserId}", userId);

                if (patient != null)
                {
                    model.PatientProfile = new PatientProfileViewModel
                    {
                        PatientID = patient.PatientID,
                        Name = patient.Name,
                        Surname = patient.Surname,
                        IDNumber = patient.IDNumber,
                        DateOfBirth = patient.DOB,
                        Cellphone = patient.CellphoneNumber,
                        Email = patient.Email,
                        HomeAddress = patient.HomeAddress
                    };

                    var requests = await _context.TestRequests
                        .Where(r => r.PatientId == patient.PatientID)
                        .Include(r => r.RequestingDoctor)
                        .Include(r => r.TestRequestItems)
                            .ThenInclude(i => i.TestType)
                        .OrderByDescending(r => r.RequestDate)
                        .ToListAsync();

                    // ---- STAT CARDS: all three buckets come from RequestBuckets ----
                    model.PatientTotalRequests = requests.Count;

                    model.PatientPendingRequests =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.Pending, r.Status));

                    // This one was never being set before, so the "In Progress" card always showed 0.
                    model.PatientInProgressRequests =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.InProgress, r.Status));

                    model.PatientResultsReady =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.Ready, r.Status));

                    // Only count abnormal results from RELEASED requests, so a patient is never
                    // alerted to a result their doctor hasn't released yet.
                    model.PatientAbnormalCount = await _context.TestResults
                        .Where(res =>
                            res.TestRequestItem.TestRequest.PatientId == patient.PatientID &&
                            RequestBuckets.Ready.Contains(res.TestRequestItem.TestRequest.Status) &&
                            res.IsAbnormal)
                        .CountAsync();

                    model.PatientRecentRequests = requests
                        .Take(5)
                        .Select(r => new TestRequestViewModel
                        {
                            RequestID = r.RequestId.ToString(),
                            RequestDate = r.RequestDate,
                            DoctorName = r.RequestingDoctor != null ? r.RequestingDoctor.FullName : "Unknown",
                            Tests = r.TestRequestItems
                                .Where(i => i.TestType != null)
                                .Select(i => i.TestType.Name)
                                .ToList(),
                            Urgency = string.IsNullOrWhiteSpace(r.Urgency) ? "Routine" : r.Urgency,
                            Status = string.IsNullOrWhiteSpace(r.Status) ? "Submitted" : r.Status
                        })
                        .ToList();
                }
            }

            // NOTE: the ConditionCount / AllergyCount / MedicationCount / UserCount /
            // RecentConditions / RecentMedications fields (the Admin section of this
            // same model) aren't populated here — they're currently defaulting to 0 /
            // empty. Say the word if you want the Admin branch wired up the same way.

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}