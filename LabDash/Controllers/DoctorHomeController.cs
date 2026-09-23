using LabDash.Areas.Identity.Data;
using LabDash.Helpers;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorHomeController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public DoctorHomeController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // DOCTOR DASHBOARD
        // GET: /DoctorHome
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();

            var vm = await BuildDashboardAsync(user.Id);

            return View(vm);
        }

        // =========================================================
        // DASHBOARD REFRESH PANEL
        // GET: /DoctorHome/RefreshPanel
        // Returns the dashboard's live panels as a partial view.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> RefreshPanel()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Unauthorized();

            var vm = await BuildDashboardAsync(user.Id);

            return PartialView("_DashboardPanels", vm);
        }

        // =========================================================
        // SHARED: BUILD THE FULL DASHBOARD VIEW MODEL
        // =========================================================

        private async Task<DoctorDashboardModel> BuildDashboardAsync(string doctorId)
        {
            var user = await _userManager.FindByIdAsync(doctorId);

            var parts = (user?.FullName ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string initials = parts.Length == 0
                ? "DR"
                : string.Join("", parts.Take(2).Select(p => p[0])).ToUpper();

            var vm = new DoctorDashboardModel
            {
                DoctorFullName = user?.FullName ?? "Doctor",
                DoctorInitials = initials
            };

            // -----------------------------------------------------
            // 1. CONSENTS — who has shared with me, and how much
            // -----------------------------------------------------

            var activeConsents = await _context.PatientDoctorConsents
                .Where(c => c.DoctorId == doctorId && c.IsActive)
                .Include(c => c.ConsentItemAccesses)
                .Include(c => c.Patient)
                .ToListAsync();

            var validConsents = activeConsents
                .Where(c => c.ConsentItemAccesses.Any())
                .ToList();

            var patientsWithAccess = validConsents
                .GroupBy(c => c.PatientID)
                .Select(g => new
                {
                    PatientID = g.Key,
                    Patient = g.First().Patient,
                    ItemCount = g.Sum(c => c.ConsentItemAccesses.Count),
                    LastGranted = g.Max(c => c.GrantedDate)
                })
                .ToList();

            vm.SharedPatientCount = patientsWithAccess.Count;
            vm.SharedItemCount = patientsWithAccess.Sum(p => p.ItemCount);

            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            vm.RecentConsentCount = validConsents
                .Count(c => c.GrantedDate >= sevenDaysAgo);

            vm.RecentSharedPatients = patientsWithAccess
                .OrderByDescending(p => p.LastGranted)
                .Take(5)
                .Select(p => new DoctorSharedPatientItem
                {
                    PatientID = p.PatientID,
                    FullName = p.Patient != null
                        ? $"{p.Patient.Name} {p.Patient.Surname}"
                        : "Unknown",
                    IDNumber = p.Patient?.IDNumber ?? "",
                    SharedItemCount = p.ItemCount,
                    LastGrantedDate = p.LastGranted
                })
                .ToList();

            // -----------------------------------------------------
            // 2. MY OWN TEST REQUESTS — status buckets
            // -----------------------------------------------------

            var myRequests = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctorId)
                .ToListAsync();

            vm.PendingRequests = myRequests
                .Count(r => RequestBuckets.In(RequestBuckets.Pending, r.Status));

            vm.InProgressRequests = myRequests
                .Count(r => RequestBuckets.In(RequestBuckets.InProgress, r.Status));

            vm.ReleasedRequests = myRequests
                .Count(r => RequestBuckets.In(RequestBuckets.Ready, r.Status));

            // -----------------------------------------------------
            // 3. ABNORMAL RESULTS on my released requests
            // -----------------------------------------------------

            vm.AbnormalResults = await _context.TestResults
                .Where(res =>
                    res.TestRequestItem.TestRequest.RequestingDoctorId == doctorId &&
                    RequestBuckets.Ready.Contains(res.TestRequestItem.TestRequest.Status) &&
                    res.IsAbnormal)
                .CountAsync();

            // -----------------------------------------------------
            // 4. NOTIFICATIONS — badge count + mini-feed
            // -----------------------------------------------------

            vm.UnreadNotificationCount = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == doctorId && !n.IsRead);

            vm.RecentNotifications = await _context.Notifications
                .Where(n => n.RecipientUserId == doctorId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new DoctorNotificationItem
                {
                    NotificationID = n.NotificationID,
                    Title = n.Title,
                    Message = n.Message,
                    LinkUrl = n.LinkUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            // -----------------------------------------------------
            // 5. CHART DATA — results captured, last 14 days
            // -----------------------------------------------------

            var fourteenDaysAgo = DateTime.Today.AddDays(-13);

            var resultsByDay = await _context.TestResults
                .Where(res =>
                    res.TestRequestItem.TestRequest.RequestingDoctorId == doctorId &&
                    res.DateCaptured >= fourteenDaysAgo)
                .GroupBy(res => res.DateCaptured.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var resultLookup = resultsByDay
                .ToDictionary(r => r.Day, r => r.Count);

            for (int i = 0; i < 14; i++)
            {
                var day = fourteenDaysAgo.AddDays(i);

                vm.ResultsTrendLabels.Add(day.ToString("dd MMM"));
                vm.ResultsTrendCounts.Add(
                    resultLookup.TryGetValue(day, out var c) ? c : 0);
            }

            // -----------------------------------------------------
            // 6. CHART DATA — consent activity, last 30 days
            // -----------------------------------------------------

            var thirtyDaysAgo = DateTime.Today.AddDays(-29);

            var consentsByDay = validConsents
                .Where(c => c.GrantedDate >= thirtyDaysAgo)
                .GroupBy(c => c.GrantedDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            for (int i = 0; i < 30; i++)
            {
                var day = thirtyDaysAgo.AddDays(i);

                vm.ConsentTrendLabels.Add(day.ToString("dd MMM"));
                vm.ConsentTrendCounts.Add(
                    consentsByDay.TryGetValue(day, out var c) ? c : 0);
            }

            return vm;
        }
    }
}