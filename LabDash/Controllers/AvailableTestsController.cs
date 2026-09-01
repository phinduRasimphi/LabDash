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

        // ==========================================================
        // AVAILABLE TESTS
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            /*
             * A technician can currently perform ANY test type.
             *
             * A test becomes available when:
             *
             * TestRequest.Status = "Samples Received"
             * AND
             * TestRequestItem.Status = "Submitted"
             *
             * We do NOT check TechnicianTestTypes for now.
             */

            var tests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.TestRequest.Status == "Samples Received")
                .OrderByDescending(x => x.TestRequest.RequestDate)
                .ToListAsync();

            return View(tests);
        }


        // ==========================================================
        // GET PATIENT / TEST DETAILS
        // ==========================================================

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

            if (item.TestRequest == null)
                return NotFound("Test request not found.");

            if (item.TestRequest.Patient == null)
                return NotFound("Patient not found.");

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
                    urgency = item.TestRequest.Urgency,
                    status = item.Status,
                    requestId = item.RequestId
                }
            });
        }


        // ==========================================================
        // ALIAS FOR YOUR EXISTING JAVASCRIPT
        // ==========================================================
        //
        // Your AvailableTests.cshtml currently calls:
        //
        // /AvailableTests/GetPatientDetails?id=...
        //
        // Therefore this action is provided as well.
        //

        [HttpGet]
        public async Task<IActionResult> GetPatientDetails(int id)
        {
            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.TestRequest == null)
                return NotFound();

            if (item.TestRequest.Patient == null)
                return NotFound();

            return Json(new
            {
                requestId = item.RequestId,

                patientName =
                    item.TestRequest.Patient.Name + " " +
                    item.TestRequest.Patient.Surname,

                idNumber = item.TestRequest.Patient.IDNumber,

                cellphone =
                    item.TestRequest.Patient.CellphoneNumber,

                email =
                    item.TestRequest.Patient.Email,

                address =
                    item.TestRequest.Patient.HomeAddress,

                allergies =
                    item.TestRequest.Patient.Allergies,

                conditions =
                    item.TestRequest.Patient.MedicalConditions,

                medication =
                    item.TestRequest.Patient.Medication,

                clinicalNotes =
                    item.TestRequest.ClinicalNotes,

                testName =
                    item.TestType.Name,

                category =
                    item.TestType.Category,

                sample =
                    item.TestType.RequiredSampleType,

                turnaround =
                    item.TestType.TurnaroundTimeHours,

                urgency =
                    item.TestRequest.Urgency,

                status =
                    item.Status
            });
        }


        // ==========================================================
        // START / SELECT TEST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            /*
             * Find the requested test.
             */

            var item = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .Include(x => x.TestType)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
            {
                TempData["Error"] =
                    "The selected laboratory test could not be found.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestRequest == null)
            {
                TempData["Error"] =
                    "The test request could not be found.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestType == null)
            {
                TempData["Error"] =
                    "The test type could not be found.";

                return RedirectToAction(nameof(Index));
            }


            // ======================================================
            // VERIFY SAMPLE WAS RECEIVED
            // ======================================================

            if (item.TestRequest.Status != "Samples Received")
            {
                TempData["Error"] =
                    "The sample for this request has not been received.";

                return RedirectToAction(nameof(Index));
            }


            // ======================================================
            // VERIFY TEST IS STILL AVAILABLE
            // ======================================================

            if (item.Status != "Submitted")
            {
                TempData["Error"] =
                    "This test is no longer available.";

                return RedirectToAction(nameof(Index));
            }


            // ======================================================
            // LOAD CONSUMABLES
            // ======================================================

            var consumables = await _context.TestTypeConsumables
                .Include(x => x.Consumable)
                .Where(x =>
                    x.TestTypeId == item.TestTypeId)
                .ToListAsync();


            // ======================================================
            // CHECK STOCK
            // ======================================================

            foreach (var stock in consumables)
            {
                if (stock.Consumable == null)
                    continue;

                if (stock.Consumable.StockLevel < stock.QuantityRequired)
                {
                    TempData["Error"] =
                        $"Not enough stock for {stock.Consumable.Name}.";

                    return RedirectToAction(nameof(Index));
                }
            }


            // ======================================================
            // DEDUCT STOCK
            // ======================================================

            foreach (var stock in consumables)
            {
                if (stock.Consumable == null)
                    continue;

                stock.Consumable.StockLevel -=
                    stock.QuantityRequired;

                stock.Consumable.UpdatedAt =
                    DateTime.Now;
            }


            // ======================================================
            // ASSIGN TEST TO TECHNICIAN
            // ======================================================

            item.AssignedTechnicianId =
                technician.Id;

            item.StartDateTime =
                DateTime.Now;

            item.Status =
                "In Progress";


            // ======================================================
            // UPDATE REQUEST STATUS
            // ======================================================
            //
            // Once at least one test has started, the request becomes
            // In Progress.
            //

            item.TestRequest.Status =
                "In Progress";


            // ======================================================
            // SAVE
            // ======================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "The test could not be started. Please try again.";

                return RedirectToAction(nameof(Index));
            }


            TempData["Success"] =
                "Test successfully assigned to you.";

            return RedirectToAction(
                nameof(ProcessTest),
                new
                {
                    id = item.TestRequestItemId
                });
        }


        // ==========================================================
        // PROCESS TEST
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> ProcessTest(int id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            var test = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(r => r.Patient)
                .Include(x => x.AssignedTechnician)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);


            if (test == null)
                return NotFound();


            // ======================================================
            // SECURITY
            // ======================================================

            if (test.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "This test is not assigned to you.";

                return RedirectToAction(nameof(Index));
            }


            // ======================================================
            // CONSUMABLES
            // ======================================================

            var consumables =
                await _context.TestTypeConsumables

                .Include(x => x.Consumable)

                .Where(x =>
                    x.TestTypeId == test.TestTypeId)

                .ToListAsync();


            ViewBag.Consumables =
                consumables;


            // ======================================================
            // PATIENT
            // ======================================================

            ViewBag.Patient =
                test.TestRequest.Patient;


            // ======================================================
            // REQUEST
            // ======================================================

            ViewBag.Request =
                test.TestRequest;


            // ======================================================
            // TURNAROUND
            // ======================================================

            var turnaround =
                TimeSpan.FromHours(
                    test.TestType.TurnaroundTimeHours);


            if (test.StartDateTime.HasValue)
            {
                ViewBag.ExpectedCompletion =
                    test.StartDateTime.Value.Add(turnaround);

                var expected =
                    test.StartDateTime.Value.Add(turnaround);

                ViewBag.TimeRemaining =
                    expected - DateTime.Now;

                ViewBag.IsOverdue =
                    DateTime.Now > expected;
            }
            else
            {
                ViewBag.ExpectedCompletion =
                    DateTime.Now.Add(turnaround);

                ViewBag.TimeRemaining =
                    turnaround;

                ViewBag.IsOverdue =
                    false;
            }


            // ======================================================
            // PROGRESS
            // ======================================================

            int progress = test.Status switch
            {
                "Submitted" => 20,
                "In Progress" => 50,
                "Completed" => 75,
                "Verified" => 100,
                "To Be Reviewed" => 60,
                _ => 0
            };

            ViewBag.Progress =
                progress;


            // ======================================================
            // TECHNICIAN STATISTICS
            // ======================================================

            ViewBag.TotalAssigned =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id);


            ViewBag.InProgress =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "In Progress");


            ViewBag.Completed =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "Completed");


            ViewBag.Verified =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "Verified");


            return View(test);
        }


        // ==========================================================
        // TESTS IN PROGRESS
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> InProgress()
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            var tests =
                await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId ==
                    technician.Id &&

                    x.Status ==
                    "In Progress")

                .OrderBy(x =>
                    x.StartDateTime)

                .ToListAsync();


            ViewBag.Total =
                tests.Count;


            ViewBag.Overdue =
                tests.Count(x =>
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


        // ==========================================================
        // COMPLETE TEST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTest(int id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            var item =
                await _context.TestRequestItems

                .Include(x => x.TestRequest)

                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);


            if (item == null)
                return NotFound();


            // ======================================================
            // ONLY ASSIGNED TECHNICIAN CAN COMPLETE
            // ======================================================

            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You can only complete tests assigned to you.";

                return RedirectToAction(
                    nameof(InProgress));
            }


            // ======================================================
            // TEST MUST BE IN PROGRESS
            // ======================================================

            if (item.Status != "In Progress")
            {
                TempData["Error"] =
                    "Only tests that are in progress can be completed.";

                return RedirectToAction(
                    nameof(InProgress));
            }


            // ======================================================
            // COMPLETE
            // ======================================================

            item.Status =
                "Completed";

            item.CompletionDateTime =
                DateTime.Now;


            // ======================================================
            // CHECK IF ALL TESTS ARE COMPLETE
            // ======================================================

            bool allComplete =
                await _context.TestRequestItems

                .Where(x =>
                    x.RequestId ==
                    item.RequestId)

                .AllAsync(x =>
                    x.Status == "Completed" ||
                    x.Status == "Verified" ||
                    x.Status == "To Be Reviewed");


            if (allComplete)
            {
                item.TestRequest.Status =
                    "Completed";
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Test marked as completed.";


            return RedirectToAction(
                nameof(Completed));
        }


        // ==========================================================
        // COMPLETED TESTS
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Completed()
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            var tests =
                await _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId ==
                    technician.Id &&

                    (
                        x.Status == "Completed" ||
                        x.Status == "Verified" ||
                        x.Status == "To Be Reviewed"
                    ))

                .OrderByDescending(x =>
                    x.CompletionDateTime)

                .ToListAsync();


            ViewBag.TotalCompleted =
                tests.Count(x =>
                    x.Status == "Completed");


            ViewBag.TotalVerified =
                tests.Count(x =>
                    x.Status == "Verified");


            ViewBag.Returned =
                tests.Count(x =>
                    x.Status == "To Be Reviewed");


            return View(tests);
        }


        // ==========================================================
        // TEST HISTORY
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> TestHistory(
            string search)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            var query =
                _context.TestRequestItems

                .Include(x => x.TestType)

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                .Where(x =>
                    x.AssignedTechnicianId ==
                    technician.Id);


            if (!string.IsNullOrWhiteSpace(search))
            {
                search =
                    search.Trim();


                query =
                    query.Where(x =>

                        x.TestRequest.Patient.Name
                            .Contains(search)

                        ||

                        x.TestRequest.Patient.Surname
                            .Contains(search)

                        ||

                        x.TestType.Name
                            .Contains(search)

                        ||

                        x.Status
                            .Contains(search));
            }


            var history =
                await query

                .OrderByDescending(x =>
                    x.CompletionDateTime ??
                    x.StartDateTime)

                .ToListAsync();


            ViewBag.TotalTests =
                history.Count;


            ViewBag.Completed =
                history.Count(x =>
                    x.Status == "Completed");


            ViewBag.Verified =
                history.Count(x =>
                    x.Status == "Verified");


            ViewBag.InProgress =
                history.Count(x =>
                    x.Status == "In Progress");


            ViewBag.Submitted =
                history.Count(x =>
                    x.Status == "Submitted");


            ViewBag.Search =
                search;


            return View(history);
        }


        // ==========================================================
        // DASHBOARD SUMMARY
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardSummary()
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();


            ViewBag.Assigned =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id);


            ViewBag.InProgress =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "In Progress");


            ViewBag.Completed =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "Completed");


            ViewBag.Verified =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "Verified");


            ViewBag.ToReview =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&
                        x.Status == "To Be Reviewed");


            ViewBag.Available =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status == "Submitted" &&
                        x.TestRequest.Status ==
                        "Samples Received");


            ViewBag.Overdue =
                await _context.TestRequestItems

                    .Include(x => x.TestType)

                    .Where(x =>
                        x.AssignedTechnicianId ==
                        technician.Id &&

                        x.Status ==
                        "In Progress" &&

                        x.StartDateTime.HasValue)

                    .CountAsync(x =>
                        DateTime.Now >
                        x.StartDateTime.Value.AddHours(
                            x.TestType.TurnaroundTimeHours));


            return View();
        }
    }
}
