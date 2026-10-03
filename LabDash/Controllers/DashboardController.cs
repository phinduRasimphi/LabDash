using System.Security.Claims;
using LabDash.Areas.Identity.Data;
using LabDash.Helpers;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly LabDbContext _context;

        public DashboardController(
            ILogger<DashboardController> logger,
            LabDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // ---------------------------------------------------------
            // DOCTOR REDIRECT
            // ---------------------------------------------------------
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Doctor"))
            {
                return RedirectToAction(
                    "Index",
                    "DoctorHome");
            }

            var model = new AdminDashboardViewModel();

            // ---------------------------------------------------------
            // ADMIN
            // ---------------------------------------------------------
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Admin"))
            {
                AdminDashboardBuilder.Populate(
                    model,
                    _context);

                model.UserCount =
                    await _context.Users.CountAsync();

                // Get the logged-in Admin's actual name
                var adminUser =
                    await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            u => u.Id == User.FindFirstValue(
                                ClaimTypes.NameIdentifier));

                if (adminUser != null)
                {
                    model.AdminName =
                        $"{adminUser.FirstName} {adminUser.LastName}".Trim();
                }
            }
            // ---------------------------------------------------------
            // PATIENT
            // ---------------------------------------------------------
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Patient"))
            {
                var userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning(
                        "Dashboard: logged-in patient has no User ID.");

                    return View(model);
                }

                // IMPORTANT:
                // AsNoTracking forces EF to read the latest Patient
                // record directly from the database.
                var patient =
                    await _context.Patients
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            p => p.UserId == userId);

                if (patient == null)
                {
                    _logger.LogWarning(
                        "Dashboard: no Patient record found for UserId {UserId}",
                        userId);

                    return View(model);
                }

                // -----------------------------------------------------
                // PATIENT PROFILE
                // -----------------------------------------------------
                model.PatientProfile =
                    new PatientProfileViewModel
                    {
                        PatientID =
                            patient.PatientID,

                        Name =
                            patient.Name ?? "",

                        Surname =
                            patient.Surname ?? "",

                        IDNumber =
                            patient.IDNumber ?? "",

                        DateOfBirth =
                            patient.DOB,

                        Cellphone =
                            patient.CellphoneNumber ?? "",

                        Email =
                            patient.Email ?? "",

                        HomeAddress =
                            patient.HomeAddress ?? "",

                        AddressLine1 =
                            patient.AddressLine1 ?? "",

                        AddressLine2 =
                            patient.AddressLine2 ?? "",

                        Suburb =
                            patient.Suburb ?? "",

                        City =
                            patient.City ?? "",

                        Province =
                            patient.Province ?? "",

                        PostalCode =
                            patient.PostalCode ?? ""
                    };

                // -----------------------------------------------------
                // TEST REQUESTS
                // -----------------------------------------------------
                var requests =
                    await _context.TestRequests
                        .AsNoTracking()
                        .Where(r =>
                            r.PatientId ==
                            patient.PatientID)
                        .Include(r =>
                            r.RequestingDoctor)
                        .Include(r =>
                            r.TestRequestItems)
                            .ThenInclude(i =>
                                i.TestType)
                        .OrderByDescending(
                            r => r.RequestDate)
                        .ToListAsync();

                // -----------------------------------------------------
                // PATIENT STATISTICS
                // -----------------------------------------------------
                model.PatientTotalRequests =
                    requests.Count;

                model.PatientPendingRequests =
                    requests.Count(r =>
                        RequestBuckets.In(
                            RequestBuckets.Pending,
                            r.Status));

                model.PatientInProgressRequests =
                    requests.Count(r =>
                        RequestBuckets.In(
                            RequestBuckets.InProgress,
                            r.Status));

                model.PatientResultsReady =
                    requests.Count(r =>
                        RequestBuckets.In(
                            RequestBuckets.Ready,
                            r.Status));

                // -----------------------------------------------------
                // ABNORMAL RESULTS
                // -----------------------------------------------------
                model.PatientAbnormalCount =
                    await _context.TestResults
                        .AsNoTracking()
                        .Where(res =>
                            res.TestRequestItem
                                .TestRequest
                                .PatientId ==
                            patient.PatientID
                            &&
                            RequestBuckets.Ready.Contains(
                                res.TestRequestItem
                                    .TestRequest
                                    .Status)
                            &&
                            res.IsAbnormal)
                        .CountAsync();

                // -----------------------------------------------------
                // RECENT REQUESTS
                // -----------------------------------------------------
                model.PatientRecentRequests =
                    requests
                        .Take(5)
                        .Select(r =>
                            new TestRequestViewModel
                            {
                                RequestID =
                                    r.RequestId.ToString(),

                                RequestDate =
                                    r.RequestDate,

                                DoctorName =
                                    r.RequestingDoctor != null
                                        ? r.RequestingDoctor.FullName
                                        : "Unknown",

                                Tests =
                                    r.TestRequestItems
                                        .Where(i =>
                                            i.TestType != null)
                                        .Select(i =>
                                            i.TestType.Name)
                                        .ToList(),

                                Urgency =
                                    string.IsNullOrWhiteSpace(
                                        r.Urgency)
                                        ? "Routine"
                                        : r.Urgency,

                                Status =
                                    string.IsNullOrWhiteSpace(
                                        r.Status)
                                        ? "Submitted"
                                        : r.Status
                            })
                        .ToList();
            }

            return View(model);
        }
    }
}