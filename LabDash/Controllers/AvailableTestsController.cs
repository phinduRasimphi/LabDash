using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        //==========================================================
        // AVAILABLE TESTS
        //==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var assignedTypes = await _context.TechnicianTestTypes
                .Where(x => x.TechnicianId == technician.Id)
                .Select(x => x.TestTypeId)
                .ToListAsync();

            var tests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .Where(x =>
                    assignedTypes.Contains(x.TestTypeId) &&
                    x.Status == "Submitted" &&
                    x.TestRequest.Status == "Samples Received")
                .OrderByDescending(x => x.TestRequest.RequestDate)
                .ToListAsync();

            return View(tests);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatient(int id)
        {
            var item = await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            return Json(new
            {
                patient = new
                {
                    name = item.TestRequest.Patient.Name,

                    surname = item.TestRequest.Patient.Surname,

                    idNumber = item.TestRequest.Patient.IDNumber,

                    cellphone = item.TestRequest.Patient.CellphoneNumber,

                    email = item.TestRequest.Patient.Email,

                    address = item.TestRequest.Patient.HomeAddress,

                    allergies = item.TestRequest.Patient.Allergies,

                    conditions = item.TestRequest.Patient.MedicalConditions,

                    medication = item.TestRequest.Patient.Medication,

                    notes = item.TestRequest.ClinicalNotes
                },

                test = new
                {
                    id = item.TestRequestItemId,

                    name = item.TestType.Name,

                    category = item.TestType.Category,

                    turnaround = item.TestType.TurnaroundTimeHours,

                    sample = item.TestType.RequiredSampleType,

                    urgency = item.TestRequest.Urgency
                }
            });
        }

        //==========================================================
        // START TEST
        //==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems

                .Include(x => x.TestRequest)

                .Include(x => x.TestType)

                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            //------------------------------------------------------
            // Enforce: technician may only start test types that
            // have been assigned to them by the lab manager
            //------------------------------------------------------

            bool isAssignedType = await _context.TechnicianTestTypes
                .AnyAsync(x =>
                    x.TechnicianId == technician.Id &&
                    x.TestTypeId == item.TestTypeId);

            if (!isAssignedType)
            {
                TempData["Error"] =
                    "You are not assigned to perform this test type.";

                return RedirectToAction(nameof(Index));
            }

            //------------------------------------------------------
            // Enforce: samples for the request must have been
            // received before a technician can select a test
            //------------------------------------------------------

            if (item.TestRequest.Status == "Pending" ||
                item.TestRequest.Status == "Submitted")
            {
                TempData["Error"] =
                    "Samples for this request have not been received yet.";

                return RedirectToAction(nameof(Index));
            }

            if (item.Status != "Submitted")
            {
                TempData["Error"] =
                    "This test has already been started.";

                return RedirectToAction(nameof(Index));
            }

            //------------------------------------------------------
            // Load consumables
            //------------------------------------------------------

            var consumables = await _context.TestTypeConsumables

                .Include(x => x.Consumable)

                .Where(x =>
                    x.TestTypeId == item.TestTypeId)

                .ToListAsync();

            //------------------------------------------------------
            // Validate stock
            //------------------------------------------------------

            foreach (var stock in consumables)
            {
                if (stock.Consumable.StockLevel < stock.QuantityRequired)
                {
                    TempData["Error"] =
                        $"Not enough stock for {stock.Consumable.Name}.";

                    return RedirectToAction(nameof(Index));
                }
            }

            //------------------------------------------------------
            // Deduct stock
            //------------------------------------------------------

            foreach (var stock in consumables)
            {
                stock.Consumable.StockLevel -= stock.QuantityRequired;
                stock.Consumable.UpdatedAt = DateTime.Now;
            }

            //------------------------------------------------------
            // Assign technician
            //------------------------------------------------------

            item.AssignedTechnicianId = technician.Id;
            item.StartDateTime = DateTime.Now;
            item.Status = "In Progress";

            //------------------------------------------------------
            // Update Request Status
            // Only if this is the first started test
            //------------------------------------------------------

            bool alreadyStarted =
                await _context.TestRequestItems
                .AnyAsync(x =>
                    x.RequestId == item.RequestId &&
                    x.TestRequestItemId != item.TestRequestItemId &&
                    (x.Status == "In Progress"
                    || x.Status == "Completed"
                    || x.Status == "Verified"));

            if (!alreadyStarted)
            {
                item.TestRequest.Status = "In Progress";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Test successfully assigned to you.";

            return RedirectToAction(nameof(ProcessTest),
                new { id = item.TestRequestItemId });
        }

        //==========================================================
        // PROCESS TEST
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> ProcessTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var test = await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(x => x.AssignedTechnician)

                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id &&
                    x.AssignedTechnicianId == technician.Id);

            if (test == null)
                return NotFound();

            //----------------------------------------------------
            // Consumables Used
            //----------------------------------------------------

            var consumables = await _context.TestTypeConsumables

                .Include(x => x.Consumable)

                .Where(x => x.TestTypeId == test.TestTypeId)

                .ToListAsync();

            //----------------------------------------------------
            // Dashboard Statistics
            //----------------------------------------------------

            ViewBag.Consumables = consumables;

            ViewBag.Patient = test.TestRequest.Patient;

            ViewBag.Request = test.TestRequest;

            //----------------------------------------------------
            // Turnaround Time
            //----------------------------------------------------

            var turnaround =
                TimeSpan.FromHours(test.TestType.TurnaroundTimeHours);

            ViewBag.ExpectedCompletion =
                test.StartDateTime?.Add(turnaround);

            //----------------------------------------------------
            // Time Remaining
            //----------------------------------------------------

            if (test.StartDateTime != null)
            {
                var expected =
                    test.StartDateTime.Value.Add(turnaround);

                ViewBag.TimeRemaining =
                    expected - DateTime.Now;

                ViewBag.IsOverdue =
                    DateTime.Now > expected;
            }
            else
            {
                ViewBag.TimeRemaining = TimeSpan.Zero;
                ViewBag.IsOverdue = false;
            }

            //----------------------------------------------------
            // Progress Percentage
            //----------------------------------------------------

            int progress = 0;

            switch (test.Status)
            {
                case "Submitted":
                    progress = 20;
                    break;

                case "In Progress":
                    progress = 50;
                    break;

                case "Completed":
                    progress = 75;
                    break;

                case "Verified":
                    progress = 100;
                    break;

                case "To Be Reviewed":
                    progress = 60;
                    break;
            }

            ViewBag.Progress = progress;

            //----------------------------------------------------
            // Technician Dashboard Cards
            //----------------------------------------------------

            ViewBag.TotalAssigned =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id);

            ViewBag.InProgress =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "In Progress");

            ViewBag.Completed =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "Completed");

            ViewBag.Verified =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "Verified");

            return View(test);
        }

        //==========================================================
        // TESTS IN PROGRESS
        //==========================================================

        [HttpGet]
        public async Task<IActionResult> InProgress()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "In Progress")

                .OrderBy(x => x.StartDateTime)

                .ToListAsync();

            ViewBag.Total = tests.Count;

            ViewBag.Overdue = tests.Count(x =>
            {
                if (!x.StartDateTime.HasValue)
                    return false;

                var expected =
                    x.StartDateTime.Value.AddHours(
                        x.TestType.TurnaroundTimeHours);

                return DateTime.Now > expected;
            });

            return View(tests);
        }

        //==========================================================
        // COMPLETE TEST
        //==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems

                .Include(x => x.TestRequest)

                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            //------------------------------------------------------
            // Enforce: only the technician assigned to this test
            // may complete it
            //------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You can only complete tests assigned to you.";

                return RedirectToAction(nameof(Index));
            }

            if (item.Status != "In Progress")
            {
                TempData["Error"] =
                    "Only tests that are in progress can be completed.";

                return RedirectToAction(nameof(InProgress));
            }

            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            //------------------------------------------------------
            // Check whether every test in request is complete
            //------------------------------------------------------

            bool allComplete =
                await _context.TestRequestItems

                .Where(x => x.RequestId == item.RequestId)

                .AllAsync(x =>
                    x.Status == "Completed"
                    || x.Status == "Verified");

            if (allComplete)
            {
                item.TestRequest.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Test marked as completed.";

            return RedirectToAction(nameof(Completed));
        }

        //==========================================================
        // COMPLETED TESTS
        //==========================================================
        [HttpGet]
        public async Task<IActionResult> Completed()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    (
                        x.Status == "Completed" ||
                        x.Status == "Verified" ||
                        x.Status == "To Be Reviewed"
                    ))

                .OrderByDescending(x => x.CompletionDateTime)

                .ToListAsync();

            ViewBag.TotalCompleted = tests.Count(x => x.Status == "Completed");

            ViewBag.TotalVerified = tests.Count(x => x.Status == "Verified");

            ViewBag.Returned =
                tests.Count(x => x.Status == "To Be Reviewed");

            return View(tests);
        }

        //==========================================================
        // TEST HISTORY
        //==========================================================

        [HttpGet]
        public async Task<IActionResult> TestHistory(string search)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var query = _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId == technician.Id);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>

                    x.TestRequest.Patient.Name.Contains(search)

                    ||

                    x.TestRequest.Patient.Surname.Contains(search)

                    ||

                    x.TestType.Name.Contains(search)

                    ||

                    x.Status.Contains(search));
            }

            var history = await query

                .OrderByDescending(x =>
                    x.CompletionDateTime ?? x.StartDateTime)

                .ToListAsync();

            ViewBag.TotalTests = history.Count;

            ViewBag.Completed =
                history.Count(x => x.Status == "Completed");

            ViewBag.Verified =
                history.Count(x => x.Status == "Verified");

            ViewBag.InProgress =
                history.Count(x => x.Status == "In Progress");

            ViewBag.Submitted =
                history.Count(x => x.Status == "Submitted");

            ViewBag.Search = search;

            return View(history);
        }

        //==========================================================
        // DASHBOARD SUMMARY
        //==========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardSummary()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            ViewBag.Assigned =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id);

            ViewBag.InProgress =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "In Progress");

            ViewBag.Completed =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "Completed");

            ViewBag.Verified =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "Verified");

            ViewBag.ToReview =
                await _context.TestRequestItems.CountAsync(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "To Be Reviewed");

            ViewBag.Overdue =
                await _context.TestRequestItems

                    .Include(x => x.TestType)

                    .Where(x =>
                        x.AssignedTechnicianId == technician.Id &&
                        x.Status == "In Progress" &&
                        x.StartDateTime.HasValue)

                    .CountAsync(x =>
                        DateTime.Now >
                        x.StartDateTime.Value.AddHours(
                            x.TestType.TurnaroundTimeHours));

            return View();
        }
    }
}