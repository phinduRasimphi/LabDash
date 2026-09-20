using System.Security.Claims;
using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly LabDbContext _context;

        public DashboardController(ILogger<DashboardController> logger, LabDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Admin section (unchanged from your original placeholder values)
            var model = new AdminDashboardViewModel
            {
                ConditionCount = 0,
                AllergyCount = 0,
                MedicationCount = 0,
                UserCount = 14
            };

            // Patient section: only runs for logged-in users in the Patient role
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var patient = !string.IsNullOrEmpty(userId)
                    ? await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId)
                    : null;

                if (patient == null)
                {
                    _logger.LogWarning("Dashboard: no Patient record linked to user {UserId}", userId);
                }
                else
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

                    // Stat cards (all three buckets come from RequestBuckets)
                    model.PatientTotalRequests = requests.Count;

                    model.PatientPendingRequests =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.Pending, r.Status));

                    model.PatientInProgressRequests =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.InProgress, r.Status));

                    model.PatientResultsReady =
                        requests.Count(r => RequestBuckets.In(RequestBuckets.Ready, r.Status));

                    // Only abnormal results from RELEASED requests are counted
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

            return View(model);
        }
    }
}