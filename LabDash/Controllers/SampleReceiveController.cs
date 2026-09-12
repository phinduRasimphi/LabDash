using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class SampleReceiveController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public SampleReceiveController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // RECEIVE SAMPLES
        // =========================================================
        // Shows requests that still have samples which have not
        // been received.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Receive()
        {
            var requests = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.Samples)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Where(r =>
                    r.Status == "Pending" ||
                    r.Status == "Partially Received")
                .OrderByDescending(r =>
                    r.Urgency == "STAT" ? 1 :
                    r.Urgency == "Urgent" ? 2 :
                    r.Urgency == "Priority" ? 3 :
                    r.Urgency == "Routine" ? 4 :
                    5)
                .ThenBy(r => r.RequestId)
                .ToListAsync();

            return View(requests);
        }


        // =========================================================
        // EDIT SAMPLE BARCODE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBarcode(
            int requestId,
            string oldBarcode,
            string newBarcode)
        {
            if (requestId <= 0)
            {
                TempData["Error"] = "Invalid test request.";
                return RedirectToAction(nameof(Receive));
            }

            if (string.IsNullOrWhiteSpace(oldBarcode))
            {
                TempData["Error"] =
                    "The existing barcode could not be identified.";

                return RedirectToAction(nameof(Receive));
            }

            if (string.IsNullOrWhiteSpace(newBarcode))
            {
                TempData["Error"] =
                    "Please enter a new barcode.";

                return RedirectToAction(nameof(Receive));
            }

            oldBarcode = oldBarcode.Trim();
            newBarcode = newBarcode.Trim();

            // -----------------------------------------------------
            // FIND SAMPLE
            // -----------------------------------------------------

            var sample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.TestRequestId == requestId &&
                    s.Barcode == oldBarcode);

            if (sample == null)
            {
                TempData["Error"] =
                    $"Sample barcode '{oldBarcode}' could not be found.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // DO NOT EDIT RECEIVED SAMPLE
            // -----------------------------------------------------

            if (sample.IsReceived)
            {
                TempData["Error"] =
                    "A sample that has already been received cannot have its barcode changed.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK DUPLICATE BARCODE
            // -----------------------------------------------------

            var barcodeExists = await _context.Samples
                .AnyAsync(s =>
                    s.Barcode == newBarcode &&
                    s.SampleId != sample.SampleId);

            if (barcodeExists)
            {
                TempData["Error"] =
                    $"Barcode '{newBarcode}' is already assigned to another sample.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK SAMPLE RECEIVE RECORDS
            // -----------------------------------------------------

            var barcodeAlreadyReceived =
                await _context.SampleReceives
                    .AnyAsync(s =>
                        s.SampleBarcode == newBarcode);

            if (barcodeAlreadyReceived)
            {
                TempData["Error"] =
                    $"Barcode '{newBarcode}' has already been used for a received sample.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // UPDATE
            // -----------------------------------------------------

            sample.Barcode = newBarcode;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "The barcode could not be updated. " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(nameof(Receive));
            }

            TempData["Success"] =
                $"Barcode successfully changed from '{oldBarcode}' to '{newBarcode}'.";

            return RedirectToAction(nameof(Receive));
        }


        // =========================================================
        // RECEIVE INDIVIDUAL SAMPLE
        // =========================================================
        //
        // IMPORTANT:
        // When a sample is received, the tests associated with
        // the request become available for the technician.
        //
        // We do NOT wait for all samples to be received.
        //
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSample(
            int requestId,
            string barcode,
            string? notes)
        {
            // -----------------------------------------------------
            // GET LOGGED-IN TECHNICIAN
            // -----------------------------------------------------

            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in technician.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // VALIDATE REQUEST
            // -----------------------------------------------------

            if (requestId <= 0)
            {
                TempData["Error"] =
                    "Invalid test request.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // VALIDATE BARCODE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(barcode))
            {
                TempData["Error"] =
                    "A sample barcode is required.";

                return RedirectToAction(nameof(Receive));
            }

            barcode = barcode.Trim();

            // -----------------------------------------------------
            // LOAD REQUEST
            // -----------------------------------------------------

            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.Samples)
                .Include(r => r.SampleReceives)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId);

            if (request == null)
            {
                TempData["Error"] =
                    $"Test request #{requestId} could not be found.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK REQUEST STATUS
            // -----------------------------------------------------

            if (request.Status != "Pending" &&
                request.Status != "Partially Received")
            {
                TempData["Error"] =
                    $"Request #{requestId} cannot receive samples. " +
                    $"Current status: {request.Status}";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // FIND SAMPLE
            // -----------------------------------------------------

            var sample = request.Samples
                .FirstOrDefault(s =>
                    s.Barcode == barcode);

            if (sample == null)
            {
                TempData["Error"] =
                    $"Barcode '{barcode}' does not belong to request #{requestId}.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK IF ALREADY RECEIVED
            // -----------------------------------------------------

            if (sample.IsReceived)
            {
                TempData["Error"] =
                    $"Sample '{barcode}' has already been received.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK SAMPLE RECEIVE RECORD
            // -----------------------------------------------------

            var alreadyReceived =
                await _context.SampleReceives
                    .AnyAsync(s =>
                        s.SampleBarcode == barcode);

            if (alreadyReceived)
            {
                TempData["Error"] =
                    $"Barcode '{barcode}' has already been recorded.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // TECHNICIAN INFORMATION
            // -----------------------------------------------------

            string technicianName =
                !string.IsNullOrWhiteSpace(technician.UserName)
                    ? technician.UserName
                    : "Laboratory Technician";

            var now = DateTime.Now;

            // -----------------------------------------------------
            // MARK SAMPLE AS RECEIVED
            // -----------------------------------------------------

            sample.IsReceived = true;
            sample.DateReceived = now;
            sample.ReceivedByTechnician = technicianName;

            // -----------------------------------------------------
            // CREATE SAMPLE RECEIVE RECORD
            // -----------------------------------------------------

            var sampleReceive = new SampleReceive
            {
                RequestId = request.RequestId,
                TechnicianName = technicianName,
                SampleBarcode = sample.Barcode,
                SampleType = sample.SampleType,
                DateTimeReceived = now,
                Status = "Samples Received",
                Notes = notes
            };

            _context.SampleReceives.Add(sampleReceive);

            // -----------------------------------------------------
            // UPDATE TEST REQUEST ITEMS
            // -----------------------------------------------------
            //
            // The tests become available after the sample is
            // received.
            //
            // We DO NOT assign a technician here.
            //
            // The technician will be assigned when they click
            // "Start Test" from Available Tests.
            //
            // -----------------------------------------------------

            foreach (var testItem in request.TestRequestItems)
            {
                if (testItem.Status == "Pending" ||
                    testItem.Status == "Requested" ||
                    testItem.Status == "Submitted" ||
                    string.IsNullOrWhiteSpace(testItem.Status))
                {
                    testItem.Status = "Submitted";

                    testItem.AssignedTechnicianId = null;
                    testItem.StartDateTime = null;
                    testItem.CompletionDateTime = null;
                }
            }

            // -----------------------------------------------------
            // CHECK WHETHER ALL SAMPLES ARE RECEIVED
            // -----------------------------------------------------

            var allSamples = request.Samples.ToList();

            bool allReceived =
                allSamples.Count > 0 &&
                allSamples.All(s => s.IsReceived);

            // -----------------------------------------------------
            // UPDATE REQUEST STATUS
            // -----------------------------------------------------

            if (allReceived)
            {
                request.Status = "Samples Received";
                request.DateTimeReceived = now;
            }
            else
            {
                request.Status = "Partially Received";
            }

            // -----------------------------------------------------
            // SAVE EVERYTHING
            // -----------------------------------------------------

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "The sample could not be received. " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // SUCCESS MESSAGE
            // -----------------------------------------------------

            if (allReceived)
            {
                TempData["Success"] =
                    $"Sample '{barcode}' was received successfully. " +
                    $"All samples for request #{request.RequestId} have been received. " +
                    $"The requested tests are now available.";
            }
            else
            {
                TempData["Success"] =
                    $"Sample '{barcode}' was received successfully. " +
                    $"The tests are now available to laboratory technicians.";
            }

            return RedirectToAction(nameof(Receive));
        }


        // =========================================================
        // AVAILABLE TESTS
        // =========================================================
        //
        // Shows tests that can be started by the logged-in
        // laboratory technician.
        //
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AvailableTests()
        {
            var tests = await _context.TestRequestItems
                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Samples)

                .Include(i => i.TestType)

                .Where(i =>
                    (
                        i.TestRequest.Status == "Partially Received" ||
                        i.TestRequest.Status == "Samples Received"
                    )
                    &&
                    (
                        i.Status == "Submitted" ||
                        i.Status == "Pending" ||
                        i.Status == "Requested"
                    )
                )

                .OrderBy(i =>
                    i.TestRequest.Urgency == "STAT" ? 1 :
                    i.TestRequest.Urgency == "Urgent" ? 2 :
                    i.TestRequest.Urgency == "Priority" ? 3 :
                    i.TestRequest.Urgency == "Routine" ? 4 :
                    5)

                .ThenBy(i => i.TestRequest.RequestId)

                .ToListAsync();

            return View(tests);
        }


        // =========================================================
        // START TEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(
            int testRequestItemId)
        {
            // -----------------------------------------------------
            // GET TECHNICIAN
            // -----------------------------------------------------

            var technician =
                await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in technician.";

                return RedirectToAction(nameof(AvailableTests));
            }

            // -----------------------------------------------------
            // LOAD TEST
            // -----------------------------------------------------

            var testItem = await _context.TestRequestItems
                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)
                .Include(i => i.TestType)
                .FirstOrDefaultAsync(i =>
                    i.TestRequestItemId == testRequestItemId);

            if (testItem == null)
            {
                TempData["Error"] =
                    "The requested test could not be found.";

                return RedirectToAction(nameof(AvailableTests));
            }

            // -----------------------------------------------------
            // CHECK STATUS
            // -----------------------------------------------------

            if (testItem.Status != "Submitted" &&
                testItem.Status != "Pending" &&
                testItem.Status != "Requested")
            {
                TempData["Error"] =
                    "This test is no longer available.";

                return RedirectToAction(nameof(AvailableTests));
            }

            // -----------------------------------------------------
            // CHECK SAMPLE
            // -----------------------------------------------------

            var hasReceivedSample = await _context.Samples
                .AnyAsync(s =>
                    s.TestRequestId == testItem.RequestId &&
                    s.IsReceived);

            if (!hasReceivedSample)
            {
                TempData["Error"] =
                    "The required sample has not been received yet.";

                return RedirectToAction(nameof(AvailableTests));
            }

            // -----------------------------------------------------
            // ASSIGN TECHNICIAN
            // -----------------------------------------------------

            testItem.AssignedTechnicianId = technician.Id;

            // -----------------------------------------------------
            // START TEST
            // -----------------------------------------------------

            testItem.Status = "In Progress";
            testItem.StartDateTime = DateTime.Now;

            // -----------------------------------------------------
            // UPDATE REQUEST STATUS
            // -----------------------------------------------------

            if (testItem.TestRequest.Status == "Partially Received" ||
                testItem.TestRequest.Status == "Samples Received")
            {
                testItem.TestRequest.Status = "In Progress";
            }

            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "The test could not be started. " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(nameof(AvailableTests));
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["Success"] =
                $"Test '{testItem.TestType?.Name ?? "Test"}' has been started.";

            return RedirectToAction(nameof(AvailableTests));
        }


        // =========================================================
        // RECEIVED SAMPLE HISTORY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Received()
        {
            var samples = await _context.SampleReceives
                .Include(s => s.TestRequest)
                    .ThenInclude(r => r.Patient)
                .OrderByDescending(s =>
                    s.DateTimeReceived)
                .ToListAsync();

            return View(samples);
        }
    }
}