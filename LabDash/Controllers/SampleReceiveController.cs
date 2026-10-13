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
        // SAMPLE RECEIVING PAGE
        // Loads submitted test requests created by TestRequestController
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Receive()
        {
            var requests = await _context.TestRequests
                .AsNoTracking()

                .Include(r => r.Patient)

                .Include(r => r.RequestingDoctor)

                .Include(r => r.Samples)

                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)

                .Where(r =>
                    r.Status == "Submitted" ||
                    r.Status == "Pending" ||
                    r.Status == "Partially Received")

                .OrderBy(r =>
                    r.Urgency == "STAT" ? 1 :
                    r.Urgency == "Urgent" ? 2 :
                    r.Urgency == "Priority" ? 3 :
                    r.Urgency == "Routine" ? 4 : 5)

                .ThenByDescending(r => r.SubmittedDate)
                .ThenByDescending(r => r.RequestDate)

                .ToListAsync();

            return View("Receive", requests);
        }


        // =========================================================
        // EDIT INDIVIDUAL SAMPLE BARCODE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBarcode(
            int requestId,
            int sampleId,
            string oldBarcode,
            string newBarcode)
        {
            if (requestId <= 0)
            {
                TempData["Error"] = "Invalid test request.";
                return RedirectToAction(nameof(Receive));
            }

            if (sampleId <= 0)
            {
                TempData["Error"] = "Invalid sample.";
                return RedirectToAction(nameof(Receive));
            }

            if (string.IsNullOrWhiteSpace(newBarcode))
            {
                TempData["Error"] = "Please enter a barcode.";
                return RedirectToAction(nameof(Receive));
            }

            oldBarcode = oldBarcode?.Trim() ?? "";
            newBarcode = newBarcode.Trim();

            var sample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.SampleId == sampleId &&
                    s.TestRequestId == requestId);

            if (sample == null)
            {
                TempData["Error"] = "The selected sample could not be found.";
                return RedirectToAction(nameof(Receive));
            }

            if (!string.Equals(
                    sample.Barcode?.Trim(),
                    oldBarcode,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The current barcode no longer matches this sample. " +
                    "Please refresh the page and try again.";

                return RedirectToAction(nameof(Receive));
            }

            if (sample.IsReceived)
            {
                TempData["Error"] =
                    "A sample that has already been received " +
                    "cannot have its barcode changed.";

                return RedirectToAction(nameof(Receive));
            }

            if (string.Equals(
                    oldBarcode,
                    newBarcode,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "No barcode changes were made.";
                return RedirectToAction(nameof(Receive));
            }

            var existingBarcodeSample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.Barcode != null &&
                    s.Barcode.Trim() == newBarcode);

            if (existingBarcodeSample == null)
            {
                TempData["Error"] =
                    $"Barcode '{newBarcode}' does not exist " +
                    "in the registered samples. " +
                    "Please check the barcode and try again.";

                return RedirectToAction(nameof(Receive));
            }

            if (existingBarcodeSample.SampleId != sample.SampleId)
            {
                TempData["Error"] =
                    $"Barcode '{newBarcode}' already belongs " +
                    "to another sample.";

                return RedirectToAction(nameof(Receive));
            }

            var barcodeAlreadyReceived = await _context.SampleReceives
                .AnyAsync(s =>
                    s.SampleBarcode != null &&
                    s.SampleBarcode.Trim() == newBarcode);

            if (barcodeAlreadyReceived)
            {
                TempData["Error"] =
                    $"Barcode '{newBarcode}' has already been " +
                    "used for a received sample.";

                return RedirectToAction(nameof(Receive));
            }

            sample.Barcode = newBarcode;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "The barcode could not be updated.";
                return RedirectToAction(nameof(Receive));
            }

            TempData["Success"] =
                $"Barcode successfully changed from " +
                $"'{oldBarcode}' to '{newBarcode}'.";

            return RedirectToAction(nameof(Receive));
        }


        // =========================================================
        // RECEIVE ONE INDIVIDUAL PHYSICAL SAMPLE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSample(
            int sampleId,
            int requestId,
            string barcode,
            string? notes)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in technician.";

                return RedirectToAction(nameof(Receive));
            }

            if (requestId <= 0)
            {
                TempData["Error"] = "Invalid test request.";
                return RedirectToAction(nameof(Receive));
            }

            if (sampleId <= 0)
            {
                TempData["Error"] = "Invalid sample.";
                return RedirectToAction(nameof(Receive));
            }

            if (string.IsNullOrWhiteSpace(barcode))
            {
                TempData["Error"] = "A sample barcode is required.";
                return RedirectToAction(nameof(Receive));
            }

            barcode = barcode.Trim();

            var request = await _context.TestRequests

                .Include(r => r.Patient)

                .Include(r => r.Samples)

                .Include(r => r.SampleReceives)

                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)

                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["Error"] =
                    $"Test request #{requestId} could not be found.";

                return RedirectToAction(nameof(Receive));
            }

            // Include "Submitted" so newly created requests are receivable.
            if (request.Status != "Submitted" &&
                request.Status != "Pending" &&
                request.Status != "Partially Received")
            {
                TempData["Error"] =
                    $"Request #{requestId} cannot receive samples. " +
                    $"Current status: {request.Status}";

                return RedirectToAction(nameof(Receive));
            }

            var sample = request.Samples
                .FirstOrDefault(s => s.SampleId == sampleId);

            if (sample == null)
            {
                TempData["Error"] =
                    $"Sample #{sampleId} does not belong " +
                    $"to request #{requestId}.";

                return RedirectToAction(nameof(Receive));
            }

            if (!string.Equals(
                    sample.Barcode?.Trim(),
                    barcode,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    $"Barcode '{barcode}' does not match " +
                    "the barcode registered for this sample.";

                return RedirectToAction(nameof(Receive));
            }

            if (sample.IsReceived)
            {
                TempData["Error"] =
                    $"Sample '{sample.Barcode}' has already been received.";

                return RedirectToAction(nameof(Receive));
            }

            var alreadyReceived = await _context.SampleReceives
                .AnyAsync(s =>
                    s.SampleBarcode != null &&
                    s.SampleBarcode.Trim() == barcode);

            if (alreadyReceived)
            {
                TempData["Error"] =
                    $"Barcode '{barcode}' has already been recorded.";

                return RedirectToAction(nameof(Receive));
            }

            string technicianName =
                !string.IsNullOrWhiteSpace(technician.UserName)
                    ? technician.UserName
                    : "Laboratory Technician";

            var now = DateTime.Now;

            sample.IsReceived = true;
            sample.DateReceived = now;
            sample.ReceivedByTechnician = technicianName;

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

            // Promote newly-available test items to Submitted.
            // Does not touch items already In Progress / Completed / Cancelled.
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


            // =========================================================
            // UPDATE REQUEST STATUS AFTER RECEIVING A SAMPLE
            // =========================================================

            var allSamples = request.Samples.ToList();

            bool allReceived =
                allSamples.Count > 0 &&
                allSamples.All(s => s.IsReceived);

            if (allReceived)
            {
                request.Status = "Samples Received";

                if (request.DateTimeReceived == null)
                {
                    request.DateTimeReceived = now;
                }
            }
            else
            {
                request.Status = "Partially Received";
            }

            // Save the sample receipt and request status together.
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "The sample receipt could not be saved. " +
                    "Please check the barcode and try again.";

                return RedirectToAction(nameof(Receive));
            }

            TempData["Success"] = allReceived
                ? $"All {allSamples.Count} sample(s) for request " +
                  $"#{request.RequestId} have been received."
                : $"Sample '{barcode}' was received successfully. " +
                  $"{allSamples.Count(s => !s.IsReceived)} sample(s) remain.";

            return RedirectToAction(nameof(Receive));
        }

        // =========================================================
        // AVAILABLE TESTS
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
                    ))

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
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in technician.";

                return RedirectToAction(nameof(AvailableTests));
            }

            var testItem = await _context.TestRequestItems

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Patient)

                .Include(i => i.TestRequest)
                    .ThenInclude(r => r.Samples)

                .Include(i => i.TestType)

                .FirstOrDefaultAsync(i =>
                    i.TestRequestItemId == testRequestItemId);

            if (testItem == null)
            {
                TempData["Error"] =
                    "The requested test could not be found.";

                return RedirectToAction(nameof(AvailableTests));
            }

            if (testItem.Status != "Submitted" &&
                testItem.Status != "Pending" &&
                testItem.Status != "Requested")
            {
                TempData["Error"] =
                    "This test is no longer available.";

                return RedirectToAction(nameof(AvailableTests));
            }

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

            testItem.AssignedTechnicianId = technician.Id;
            testItem.Status = "In Progress";
            testItem.StartDateTime = DateTime.Now;

            if (testItem.TestRequest.Status == "Partially Received" ||
                testItem.TestRequest.Status == "Samples Received")
            {
                testItem.TestRequest.Status = "In Progress";
            }

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

            TempData["Success"] =
                $"Test '{testItem.TestType?.Name ?? "Test"}' " +
                "has been started.";

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

                .OrderByDescending(s => s.DateTimeReceived)

                .ToListAsync();

            return View(samples);
        }
    }
}