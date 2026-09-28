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
        public async Task<IActionResult> Index()
        {
            var tests = await _context.TestRequestItems

                // -----------------------------------------------------
                // TEST TYPE
                // -----------------------------------------------------

                .Include(x => x.TestType)

                // -----------------------------------------------------
                // TEST REQUEST + PATIENT
                // -----------------------------------------------------

                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)

                // -----------------------------------------------------
                // ONLY TESTS THAT ARE STILL AVAILABLE
                // -----------------------------------------------------

                .Where(x =>
                    x.Status == "Submitted"

                    &&

                    x.TestRequest != null

                    &&

                    // -------------------------------------------------
                    // IMPORTANT:
                    // A sample belonging to this request must have
                    // actually been received.
                    //
                    // This works even when the request status is:
                    //
                    // Pending
                    // Partially Received
                    // Samples Received
                    // -------------------------------------------------

                    _context.Samples.Any(s =>
                        s.TestRequestId == x.RequestId &&
                        s.IsReceived
                    )
                )

                // =====================================================
                // SORT BY URGENCY
                // =====================================================

                .OrderBy(x =>
                    x.TestRequest.Urgency == "STAT" ? 1 :
                    x.TestRequest.Urgency == "Urgent" ? 2 :
                    x.TestRequest.Urgency == "Priority" ? 3 :
                    x.TestRequest.Urgency == "Routine" ? 4 :
                    5)

                // =====================================================
                // OLDEST REQUEST FIRST
                // =====================================================

                .ThenBy(x => x.TestRequest.RequestDate)

                // =====================================================
                // OLDEST TEST ITEM FIRST
                // =====================================================

                .ThenBy(x => x.TestRequestItemId)

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
                .FirstOrDefaultAsync(x =>
                    x.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.TestRequest == null)
                return NotFound("Test request not found.");

            if (item.TestRequest.Patient == null)
                return NotFound("Patient not found.");

            // =========================================================
            // LOAD REQUIRED CONSUMABLES
            // =========================================================

            var consumables = await _context.TestTypeConsumables
                .Include(x => x.Consumable)
                .Where(x => x.TestTypeId == item.TestTypeId)
                .Select(x => new
                {
                    name = x.Consumable != null
                        ? x.Consumable.Name
                        : "Unknown Consumable",

                    required = x.QuantityRequired,

                    available = x.Consumable != null
                        ? x.Consumable.StockLevel
                        : 0,

                    isAvailable =
                        x.Consumable != null &&
                        x.Consumable.StockLevel >= x.QuantityRequired
                })
                .ToListAsync();

            // =========================================================
            // CHECK WHETHER ALL CONSUMABLES ARE AVAILABLE
            // =========================================================

            bool allConsumablesAvailable =
                consumables.All(x => x.isAvailable);

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
                        item.TestType?.TurnaroundTimeMinutes,

                    sample =
                        item.TestType?.RequiredSampleType,

                    urgency =
                        item.TestRequest.Urgency,

                    status =
                        item.Status,

                    requestId =
                        item.RequestId
                },

                // =====================================================
                // CONSUMABLE INFORMATION
                // =====================================================

                consumables = consumables,

                allConsumablesAvailable =
                    allConsumablesAvailable
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
                    item.TestType?.Name,

                category =
                    item.TestType?.Category,

                sample =
                    item.TestType?.RequiredSampleType,

                turnaround =
                    item.TestType?.TurnaroundTimeMinutes,

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

            // =====================================================
            // CHECK TEST REQUEST
            // =====================================================

            if (item.TestRequest == null)
            {
                TempData["Error"] =
                    "The test request could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK TEST TYPE
            // =====================================================

            if (item.TestType == null)
            {
                TempData["Error"] =
                    "The test type could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK TEST STATUS
            // =====================================================

            if (item.Status != "Submitted")
            {
                TempData["Error"] =
                    "This test is no longer available to start.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK ACTUAL SAMPLE RECEIPT
            // =====================================================
            // IMPORTANT:
            // Do NOT check TestRequest.Status here.
            //
            // A request can be "Partially Received" while the
            // particular sample required for this test has already
            // been received.
            // =====================================================

            var sampleReceived = await _context.Samples
                .AnyAsync(s =>
                    s.TestRequestId == item.RequestId &&
                    s.IsReceived);

            if (!sampleReceived)
            {
                TempData["Error"] =
                    "The sample for this test has not been received yet.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // CHECK CONSUMABLE STOCK
            // =====================================================

            var consumables =
                await _context.TestTypeConsumables
                    .Include(x => x.Consumable)
                    .Where(x =>
                        x.TestTypeId == item.TestTypeId)
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
            // DEDUCT CONSUMABLE STOCK
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

            // =====================================================
            // START DATE/TIME
            // =====================================================

            item.StartDateTime =
                DateTime.Now;

            // =====================================================
            // CHANGE TEST STATUS
            // =====================================================

            item.Status =
                "In Progress";

            // =====================================================
            // CHANGE REQUEST STATUS
            // =====================================================

            item.TestRequest.Status =
                "In Progress";

            // =====================================================
            // SAVE CHANGES
            // =====================================================

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

            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "Test successfully started and assigned to you.";

            // =====================================================
            // OPEN PROCESS TEST
            // =====================================================

            return RedirectToAction(
                nameof(ProcessTest),
                new
                {
                    id = item.TestRequestItemId
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

            double turnaroundMinutes = 0;

            if (item.TestType != null)
            {
                turnaroundMinutes =
                    item.TestType.TurnaroundTimeMinutes;
            }

            ViewBag.TurnaroundMinutes =
                turnaroundMinutes;

            // =========================================================
            // DUE DATE
            // =========================================================

            DateTime? dueDateTime = null;

            if (item.StartDateTime.HasValue &&
                turnaroundMinutes > 0)
            {
                dueDateTime =
                    item.StartDateTime.Value
                        .AddMinutes(turnaroundMinutes);
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
        // TECHNICIAN DASHBOARD SUMMARY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardSummary()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var now = DateTime.Now;
            var nearDeadline = now.AddMinutes(30);

            // =========================================================
            // 1. TESTS WAITING TO BE SELECTED
            // =========================================================
            //
            // Test must:
            // - still be Submitted
            // - have a received sample
            // - not already be assigned
            //
            var waitingToBeSelected =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status == "Submitted" &&
                        x.AssignedTechnicianId == null &&
                        x.TestRequest != null &&
                        _context.Samples.Any(s =>
                            s.TestRequestId == x.RequestId &&
                            s.IsReceived)
                    );


            // =========================================================
            // 2. TESTS SELECTED BY CURRENT TECHNICIAN
            // =========================================================
            //
            // These are tests currently being processed.
            //
            var selectedByTechnician =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId == technician.Id &&
                        x.Status == "In Progress"
                    );


            // =========================================================
            // 3. TESTS WAITING FOR VERIFICATION
            // =========================================================
            //
            // Completed by this technician but not yet verified.
            //
            var waitingForVerification =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId == technician.Id &&
                        x.Status == "Completed"
                    );


            // =========================================================
            // 4. TESTS WAITING FOR REVIEW
            // =========================================================
            //
            // These are tests that were sent back after verification.
            //
            var waitingForReview =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status == "To Be Reviewed"
                    );


            // =========================================================
            // 5. URGENT / STAT TESTS
            // =========================================================
            //
            // A STAT test is included when it is currently actionable:
            //
            // - waiting to be selected
            // - in progress
            // - waiting for verification
            // - waiting for review
            //
            var urgent =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.TestRequest != null &&
                        x.TestRequest.Urgency == "STAT" &&
                        (
                            // Waiting to be selected
                            (
                                x.Status == "Submitted" &&
                                x.AssignedTechnicianId == null &&
                                _context.Samples.Any(s =>
                                    s.TestRequestId == x.RequestId &&
                                    s.IsReceived)
                            )

                            ||

                            // Selected by current technician
                            (
                                x.Status == "In Progress" &&
                                x.AssignedTechnicianId == technician.Id
                            )

                            ||

                            // Waiting for verification
                            (
                                x.Status == "Completed" &&
                                x.AssignedTechnicianId == technician.Id
                            )

                            ||

                            // Waiting for review
                            x.Status == "To Be Reviewed"
                        )
                    );


            // =========================================================
            // 6. OVERDUE TESTS
            // =========================================================
            //
            // Turnaround time is calculated from StartDateTime.
            //
            var overdue =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId == technician.Id &&
                        x.Status == "In Progress" &&
                        x.StartDateTime.HasValue &&
                        x.TestType != null &&
                        x.TestType.TurnaroundTimeMinutes > 0 &&
                        x.StartDateTime.Value.AddMinutes(
                            x.TestType.TurnaroundTimeMinutes
                        ) < now
                    );


            // =========================================================
            // 7. TESTS NEARING TURNAROUND LIMIT
            // =========================================================
            //
            // Due within the next 30 minutes.
            //
            var nearDeadlineCount =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.AssignedTechnicianId == technician.Id &&
                        x.Status == "In Progress" &&
                        x.StartDateTime.HasValue &&
                        x.TestType != null &&
                        x.TestType.TurnaroundTimeMinutes > 0 &&

                        x.StartDateTime.Value.AddMinutes(
                            x.TestType.TurnaroundTimeMinutes
                        ) >= now &&

                        x.StartDateTime.Value.AddMinutes(
                            x.TestType.TurnaroundTimeMinutes
                        ) <= nearDeadline
                    );


            // =========================================================
            // 8. VERIFIED TESTS
            // =========================================================
            //
            // Kept as an additional dashboard statistic.
            //
            var verified =
                await _context.TestRequestItems
                    .CountAsync(x =>
                        x.Status == "Verified"
                    );


            // =========================================================
            // RETURN DASHBOARD DATA
            // =========================================================

            return Json(new
            {
                waitingToBeSelected = waitingToBeSelected,

                selectedByTechnician = selectedByTechnician,

                waitingForVerification = waitingForVerification,

                waitingForReview = waitingForReview,

                urgent = urgent,

                overdue = overdue,

                nearDeadline = nearDeadlineCount,

                verified = verified
            });
        }


        // =========================================================
        // DASHBOARD CATEGORIES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardCategories()
        {
            var categories =
                await _context.TestTypes
                    .Where(x =>
                        x.Category != null &&
                        x.Category != "")
                    .Select(x => x.Category)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            return Json(categories);
        }


        // =========================================================
        // TECHNICIAN DASHBOARD QUEUE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DashboardQueue(
            string? urgency,
            string? category,
            string? due,
            string? requestNumber)
        {
            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
                return Unauthorized();

            var now = DateTime.Now;


            // =========================================================
            // BASE DASHBOARD QUEUE
            // =========================================================

            var query =
                _context.TestRequestItems

                    .Include(x => x.TestType)

                    .Include(x => x.TestRequest)
                        .ThenInclude(x => x.Patient)

                    .Where(x =>
                        x.TestRequest != null &&

                        (
                            // =================================================
                            // WAITING TO BE SELECTED
                            // =================================================
                            (
                                x.Status == "Submitted" &&
                                x.AssignedTechnicianId == null &&

                                _context.Samples.Any(s =>
                                    s.TestRequestId == x.RequestId &&
                                    s.IsReceived)
                            )

                            ||

                            // =================================================
                            // SELECTED BY CURRENT TECHNICIAN
                            // =================================================
                            (
                                x.Status == "In Progress" &&
                                x.AssignedTechnicianId == technician.Id
                            )

                            ||

                            // =================================================
                            // WAITING FOR VERIFICATION
                            // =================================================
                            (
                                x.Status == "Completed" &&
                                x.AssignedTechnicianId == technician.Id
                            )

                            ||

                            // =================================================
                            // WAITING FOR REVIEW
                            // =================================================
                            (
                                x.Status == "To Be Reviewed"
                            )
                        )
                    )
                    .AsQueryable();


            // =========================================================
            // URGENCY FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(urgency))
            {
                query = query.Where(x =>
                    x.TestRequest!.Urgency == urgency);
            }


            // =========================================================
            // CATEGORY FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x =>
                    x.TestType != null &&
                    x.TestType.Category == category);
            }


            // =========================================================
            // REQUEST NUMBER FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(requestNumber))
            {
                query = query.Where(x =>
                    x.RequestId
                        .ToString()
                        .Contains(requestNumber));
            }


            // =========================================================
            // LOAD TESTS
            // =========================================================

            var tests =
                await query
                    .OrderBy(x =>
                        x.TestRequest!.Urgency == "STAT"
                            ? 1
                            : x.TestRequest.Urgency == "Urgent"
                                ? 2
                                : x.TestRequest.Urgency == "Priority"
                                    ? 3
                                    : 4
                    )
                    .ThenBy(x =>
                        x.TestRequest!.RequestDate)
                    .ThenBy(x =>
                        x.TestRequestItemId)
                    .ToListAsync();


            // =========================================================
            // BUILD DASHBOARD RESULT
            // =========================================================

            var result = tests
                .Select(x =>
                {
                    DateTime? dueDateTime = null;

                    // Turnaround applies to tests currently in progress.
                    if (
                        x.Status == "In Progress" &&
                        x.StartDateTime.HasValue &&
                        x.TestType != null &&
                        x.TestType.TurnaroundTimeMinutes > 0)
                    {
                        dueDateTime =
                            x.StartDateTime.Value.AddMinutes(
                                x.TestType.TurnaroundTimeMinutes);
                    }


                    // =====================================================
                    // OVERDUE
                    // =====================================================

                    bool isOverdue =
                        dueDateTime.HasValue &&
                        dueDateTime.Value < now;


                    // =====================================================
                    // NEARING LIMIT
                    // =====================================================

                    bool isNearDeadline =
                        dueDateTime.HasValue &&
                        dueDateTime.Value >= now &&
                        dueDateTime.Value <= now.AddMinutes(30);


                    // =====================================================
                    // TIME REMAINING
                    // =====================================================

                    int? minutesRemaining = null;

                    if (dueDateTime.HasValue)
                    {
                        minutesRemaining =
                            (int)Math.Ceiling(
                                (dueDateTime.Value - now)
                                    .TotalMinutes);
                    }


                    // =====================================================
                    // STATUS DISPLAY
                    // =====================================================

                    string statusDisplay;

                    switch (x.Status)
                    {
                        case "Submitted":
                            statusDisplay = "Waiting to be Selected";
                            break;

                        case "In Progress":
                            statusDisplay = "Selected / In Progress";
                            break;

                        case "Completed":
                            statusDisplay = "Waiting for Verification";
                            break;

                        case "To Be Reviewed":
                            statusDisplay = "Waiting for Review";
                            break;

                        case "Verified":
                            statusDisplay = "Verified";
                            break;

                        default:
                            statusDisplay = x.Status;
                            break;
                    }


                    // =====================================================
                    // RETURN DASHBOARD ITEM
                    // =====================================================

                    return new
                    {
                        id = x.TestRequestItemId,

                        requestId = x.RequestId,

                        patient =
                            x.TestRequest!.Patient != null
                                ? x.TestRequest.Patient.Name +
                                  " " +
                                  x.TestRequest.Patient.Surname
                                : "Unknown Patient",

                        testName =
                            x.TestType?.Name ??
                            "Unknown Test",

                        category =
                            x.TestType?.Category ??
                            "Uncategorised",

                        urgency =
                            x.TestRequest?.Urgency ??
                            "Routine",

                        status = x.Status,

                        statusDisplay = statusDisplay,

                        assigned =
                            x.AssignedTechnicianId ==
                            technician.Id,

                        startTime =
                            x.StartDateTime,

                        dueTime =
                            dueDateTime,

                        dueDisplay =
                            dueDateTime.HasValue
                                ? dueDateTime.Value
                                    .ToString("dd MMM yyyy HH:mm")
                                : "—",

                        overdue = isOverdue,

                        nearDeadline = isNearDeadline,

                        minutesRemaining = minutesRemaining,

                        turnaroundMinutes =
                            x.TestType != null
                                ? x.TestType.TurnaroundTimeMinutes
                                : 0,

                        actionId =
                            x.TestRequestItemId
                    };
                })
                .ToList();


            // =========================================================
            // DUE-TIME FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(due))
            {
                switch (due.ToLower())
                {
                    case "overdue":

                        result = result
                            .Where(x => x.overdue)
                            .ToList();

                        break;


                    case "near":

                        result = result
                            .Where(x => x.nearDeadline)
                            .ToList();

                        break;


                    case "ontime":

                        result = result
                            .Where(x =>
                                !x.overdue &&
                                !x.nearDeadline)
                            .ToList();

                        break;


                    case "today":

                        result = result
                            .Where(x =>
                                x.dueTime.HasValue &&
                                x.dueTime.Value.Date ==
                                now.Date)
                            .ToList();

                        break;
                }
            }


            // =========================================================
            // RETURN QUEUE
            // =========================================================

            return Json(result);
        }
    }
}