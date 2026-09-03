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

        // =========================================================
        // AVAILABLE TESTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // IMPORTANT:
            //
            // A request can be:
            // Samples Received
            // OR
            // In Progress
            //
            // We must NOT only check Samples Received because
            // starting one test changes the request to In Progress.
            //
            // Individual test status must be Submitted.

            var tests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Samples)
                .Include(x => x.AssignedTechnician)
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.TestRequest != null &&
                    (
                        x.TestRequest.Status ==
                            "Samples Received"
                        ||
                        x.TestRequest.Status ==
                            "In Progress"
                    ))
                .OrderByDescending(
                    x => x.TestRequest.RequestDate)
                .ThenByDescending(
                    x => x.TestRequestItemId)
                .ToListAsync();

            return View(tests);
        }

        // =========================================================
        // GET PATIENT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetPatient(int id)
        {
            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .FirstOrDefaultAsync(
                    x => x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.TestRequest == null)
                return NotFound(
                    "Test request not found.");

            if (item.TestRequest.Patient == null)
                return NotFound(
                    "Patient not found.");

            return Json(new
            {
                patient = new
                {
                    name =
                        item.TestRequest.Patient.Name,

                    surname =
                        item.TestRequest.Patient.Surname,

                    idNumber =
                        item.TestRequest.Patient.IDNumber,

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

                    notes =
                        item.TestRequest.ClinicalNotes
                },

                test = new
                {
                    id =
                        item.TestRequestItemId,

                    name =
                        item.TestType?.Name,

                    category =
                        item.TestType?.Category,

                    turnaround =
                        item.TestType?.TurnaroundTimeHours,

                    sample =
                        item.TestType?.RequiredSampleType,

                    urgency =
                        item.TestRequest.Urgency,

                    status =
                        item.Status,

                    requestId =
                        item.RequestId
                }
            });
        }

        // =========================================================
        // GET PATIENT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetPatientDetails(
            int id)
        {
            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .FirstOrDefaultAsync(
                    x => x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.TestRequest == null)
                return NotFound();

            if (item.TestRequest.Patient == null)
                return NotFound();

            return Json(new
            {
                requestId =
                    item.RequestId,

                patientName =
                    item.TestRequest.Patient.Name +
                    " " +
                    item.TestRequest.Patient.Surname,

                idNumber =
                    item.TestRequest.Patient.IDNumber,

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
                    item.TestType?.Name,

                category =
                    item.TestType?.Category,

                sample =
                    item.TestType?.RequiredSampleType,

                turnaround =
                    item.TestType?.TurnaroundTimeHours,

                urgency =
                    item.TestRequest.Urgency,

                status =
                    item.Status
            });
        }

        // =========================================================
        // START TEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // =====================================================
            // LOAD TEST
            // =====================================================

            var item = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .Include(x => x.TestType)
                .FirstOrDefaultAsync(
                    x => x.TestRequestItemId == id);

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

            // =====================================================
            // CHECK REQUEST STATUS
            // =====================================================

            if (item.TestRequest.Status != "Samples Received" &&
                item.TestRequest.Status != "In Progress")
            {
                TempData["Error"] =
                    "The sample for this request has not been received.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK TEST STATUS
            // =====================================================

            if (item.Status != "Submitted")
            {
                TempData["Error"] =
                    "This test is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK STOCK
            // =====================================================

            var consumables =
                await _context.TestTypeConsumables
                    .Include(x => x.Consumable)
                    .Where(x =>
                        x.TestTypeId ==
                        item.TestTypeId)
                    .ToListAsync();

            foreach (var stock in consumables)
            {
                if (stock.Consumable == null)
                    continue;

                if (stock.Consumable.StockLevel <
                    stock.QuantityRequired)
                {
                    TempData["Error"] =
                        $"Not enough stock for " +
                        $"{stock.Consumable.Name}.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // =====================================================
            // DEDUCT STOCK
            // =====================================================

            foreach (var stock in consumables)
            {
                if (stock.Consumable == null)
                    continue;

                stock.Consumable.StockLevel -=
                    stock.QuantityRequired;

                stock.Consumable.UpdatedAt =
                    DateTime.Now;
            }

            // =====================================================
            // ASSIGN TECHNICIAN
            // =====================================================

            item.AssignedTechnicianId =
                technician.Id;

            item.StartDateTime =
                DateTime.Now;

            item.Status =
                "In Progress";

            // =====================================================
            // UPDATE REQUEST STATUS
            // =====================================================

            item.TestRequest.Status =
                "In Progress";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "The test could not be started. " +
                    (ex.InnerException?.Message ??
                     ex.Message);

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Test successfully assigned to you.";

            return RedirectToAction(
                nameof(ProcessTest),
                new
                {
                    id =
                        item.TestRequestItemId
                });
        }

        // =========================================================
        // IN PROGRESS TESTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> InProgress()
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .Where(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    x.Status == "In Progress")
                .OrderByDescending(x => x.StartDateTime)
                .ToListAsync();

            return View(tests);
        }

        // =========================================================
        // PROCESS TEST
        // =========================================================

        // =========================================================
        // PROCESS TEST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ProcessTest(int id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // =========================================================
            // LOAD TEST
            // =========================================================

            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .FirstOrDefaultAsync(
                    x => x.TestRequestItemId == id);

            if (item == null)
            {
                TempData["Error"] =
                    "Test could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // CHECK REQUEST
            // =========================================================

            if (item.TestRequest == null)
            {
                TempData["Error"] =
                    "The test request could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // CHECK TECHNICIAN
            // =========================================================

            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to this test.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // CHECK STATUS
            // =========================================================

            if (item.Status != "In Progress")
            {
                TempData["Error"] =
                    "This test is not currently in progress.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // LOAD CONSUMABLES
            // =========================================================

            var consumables = await _context.TestTypeConsumables
                .Include(x => x.Consumable)
                .Where(x =>
                    x.TestTypeId == item.TestTypeId)
                .ToListAsync();

            // IMPORTANT:
            // ProcessTest.cshtml uses ViewBag.Consumables
            ViewBag.Consumables = consumables;

            // =========================================================
            // TURNAROUND TIME
            // =========================================================

            double turnaroundHours = 0;

            if (item.TestType != null)
            {
                turnaroundHours =
                    item.TestType.TurnaroundTimeHours;
            }

            ViewBag.TurnaroundHours =
                turnaroundHours;

            // =========================================================
            // DUE DATE
            // =========================================================

            DateTime? dueDateTime = null;

            if (item.StartDateTime.HasValue &&
                turnaroundHours > 0)
            {
                dueDateTime =
                    item.StartDateTime.Value
                        .AddHours(turnaroundHours);
            }

            ViewBag.DueDateTime =
                dueDateTime;

            // =========================================================
            // CHECK OVERDUE
            // =========================================================

            bool isOverdue = false;

            if (dueDateTime.HasValue)
            {
                isOverdue =
                    DateTime.Now > dueDateTime.Value;
            }

            ViewBag.IsOverdue =
                isOverdue;

            // =========================================================
            // TIME REMAINING
            // =========================================================

            TimeSpan? timeRemaining = null;

            if (dueDateTime.HasValue)
            {
                timeRemaining =
                    dueDateTime.Value - DateTime.Now;
            }

            ViewBag.TimeRemaining =
                timeRemaining;

            // =========================================================
            // PROGRESS PERCENTAGE
            // =========================================================

            int progressPercentage = 0;

            if (item.StartDateTime.HasValue &&
                dueDateTime.HasValue)
            {
                double totalSeconds =
                    (dueDateTime.Value -
                     item.StartDateTime.Value)
                    .TotalSeconds;

                double elapsedSeconds =
                    (DateTime.Now -
                     item.StartDateTime.Value)
                    .TotalSeconds;

                if (totalSeconds > 0)
                {
                    progressPercentage =
                        (int)(
                            (elapsedSeconds /
                             totalSeconds) * 100
                        );
                }

                if (progressPercentage < 0)
                    progressPercentage = 0;

                if (progressPercentage > 100)
                    progressPercentage = 100;
            }

            ViewBag.ProgressPercentage =
                progressPercentage;

            // =========================================================
            // PATIENT
            // =========================================================

            ViewBag.Patient =
                item.TestRequest.Patient;

            // =========================================================
            // TEST REQUEST
            // =========================================================

            ViewBag.TestRequest =
                item.TestRequest;

            // =========================================================
            // TEST TYPE
            // =========================================================

            ViewBag.TestType =
                item.TestType;

            // =========================================================
            // TEST ITEM
            // =========================================================

            ViewBag.TestItem =
                item;

            // =========================================================
            // TECHNICIAN
            // =========================================================

            ViewBag.Technician =
                technician;

            // =========================================================
            // RETURN VIEW
            // =========================================================

            return View(item);
        }

        // =========================================================
        // COMPLETE TEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTest(
            int id)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(x => x.TestRequest)
                .FirstOrDefaultAsync(
                    x => x.TestRequestItemId == id);

            if (item == null)
            {
                TempData["Error"] =
                    "Test could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // SECURITY CHECK
            // =====================================================

            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to this test.";

                return RedirectToAction(nameof(Index));
            }

            if (item.Status != "In Progress")
            {
                TempData["Error"] =
                    "Only tests currently in progress " +
                    "can be completed.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // COMPLETE TEST
            // =====================================================

            item.Status =
                "Completed";

            item.CompletionDateTime =
                DateTime.Now;

            // =====================================================
            // CHECK ALL TESTS FOR REQUEST
            // =====================================================

            var requestItems =
                await _context.TestRequestItems
                    .Where(x =>
                        x.RequestId ==
                        item.RequestId)
                    .ToListAsync();

            bool allFinished =
                requestItems.Count > 0 &&
                requestItems.All(x =>
                    x.Status == "Completed" ||
                    x.Status == "Verified" ||
                    x.Status == "To Be Reviewed");

            if (allFinished &&
                item.TestRequest != null)
            {
                item.TestRequest.Status =
                    "Completed";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "The test could not be completed. " +
                    (ex.InnerException?.Message ??
                     ex.Message);

                return RedirectToAction(
                    nameof(ProcessTest),
                    new { id });
            }

            TempData["Success"] =
                "Laboratory test completed successfully.";

            return RedirectToAction(
                nameof(Completed));
        }

        // =========================================================
        // COMPLETED TESTS
        // =========================================================

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
                            technician.Id
                        &&
                        (
                            x.Status ==
                                "Completed"
                            ||
                            x.Status ==
                                "Verified"
                            ||
                            x.Status ==
                                "To Be Reviewed"
                        ))
                    .OrderByDescending(
                        x => x.CompletionDateTime)
                    .ToListAsync();

            return View(tests);
        }
        // =========================================================
        // VIEW COMPLETED TEST DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> CompletedDetails(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Include(x => x.AssignedTechnician)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
            {
                TempData["Error"] = "The laboratory test could not be found.";
                return RedirectToAction(nameof(Completed));
            }

            // Only allow the technician assigned to this test
            if (item.AssignedTechnicianId != technician.Id)
            {
                TempData["Error"] =
                    "You are not assigned to this laboratory test.";

                return RedirectToAction(nameof(Completed));
            }

            // Only completed/history statuses can be viewed here
            if (item.Status != "Completed" &&
                item.Status != "Verified" &&
                item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is not available in the completed test history.";

                return RedirectToAction(nameof(Completed));
            }

            // Load laboratory result
            var result = await _context.TestResults
                .Include(x => x.CapturedByTechnician)
                .Include(x => x.VerifiedByTechnician)
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == item.TestRequestItemId);

            // Load latest verification/review information
            var verification = await _context.TestVerifications
                .Include(x => x.VerifiedByTechnician)
                .Where(x =>
                    x.TestRequestItemId == item.TestRequestItemId)
                .OrderByDescending(x => x.VerificationDate)
                .FirstOrDefaultAsync();

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.Result = result;
            ViewBag.Verification = verification;
            ViewBag.CurrentTechnician = technician;

            return View(item);
        }

        // =========================================================
        // TEST HISTORY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> TestHistory()
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
                        technician.Id)
                    .OrderByDescending(
                        x => x.StartDateTime)
                    .ToListAsync();

            return View(tests);
        }

        // =========================================================
        // DASHBOARD SUMMARY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardSummary()
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // -----------------------------------------------------
            // AVAILABLE
            // -----------------------------------------------------

            ViewBag.Available =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status == "Submitted" &&
                        x.TestRequest != null &&
                        (
                            x.TestRequest.Status ==
                                "Samples Received"
                            ||
                            x.TestRequest.Status ==
                                "In Progress"
                        ));

            // -----------------------------------------------------
            // IN PROGRESS
            // -----------------------------------------------------

            ViewBag.InProgress =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                            technician.Id &&
                        x.Status ==
                            "In Progress");

            // -----------------------------------------------------
            // COMPLETED
            // -----------------------------------------------------

            ViewBag.Completed =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId ==
                            technician.Id &&
                        x.Status ==
                            "Completed");

            // -----------------------------------------------------
            // VERIFIED
            // -----------------------------------------------------

            ViewBag.Verified =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status ==
                            "Verified");

            // -----------------------------------------------------
            // TO BE REVIEWED
            // -----------------------------------------------------

            ViewBag.ToBeReviewed =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status ==
                            "To Be Reviewed");

            // -----------------------------------------------------
            // URGENT
            // -----------------------------------------------------

            ViewBag.Urgent =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.TestRequest != null &&
                        x.TestRequest.Urgency ==
                            "STAT" &&
                        (
                            x.Status ==
                                "Submitted"
                            ||
                            x.Status ==
                                "In Progress"
                        ));

            // -----------------------------------------------------
            // RETURN JSON
            // -----------------------------------------------------

            return Json(new
            {
                available =
                    ViewBag.Available,

                inProgress =
                    ViewBag.InProgress,

                completed =
                    ViewBag.Completed,

                verified =
                    ViewBag.Verified,

                toBeReviewed =
                    ViewBag.ToBeReviewed,

                urgent =
                    ViewBag.Urgent
            });
        }
    }
}