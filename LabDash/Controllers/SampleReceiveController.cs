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
        // =========================================================
        // Shows pending and partially received requests.
        // The technician can see all request/sample information
        // and receive individual samples from this page.
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
        // EDIT BARCODE
        // =========================================================
        // Allows technician to correct/edit a sample barcode
        // before receiving the sample.
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
                TempData["Error"] =
                    "Invalid test request.";

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
            // LOAD SAMPLE
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
            // DO NOT EDIT A SAMPLE THAT HAS ALREADY BEEN RECEIVED
            // -----------------------------------------------------

            if (sample.IsReceived)
            {
                TempData["Error"] =
                    "A sample that has already been received cannot have its barcode changed.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK IF NEW BARCODE ALREADY EXISTS
            // -----------------------------------------------------

            var barcodeExists = await _context.Samples
                .AnyAsync(s =>
                    s.Barcode == newBarcode &&
                    s.TestRequestId != requestId);

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
            // UPDATE BARCODE
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSample(
            int requestId,
            string barcode,
            string? notes)
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
            // CHECK ALREADY RECEIVED
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
            // TECHNICIAN
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
            // CHECK ALL SAMPLES
            // -----------------------------------------------------

            var allSamples = await _context.Samples
                .Where(s =>
                    s.TestRequestId == request.RequestId)
                .ToListAsync();

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

                // -------------------------------------------------
                // MAKE TESTS AVAILABLE
                // -------------------------------------------------

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
            }
            else
            {
                request.Status = "Partially Received";
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
                    "The sample could not be received. " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            if (allReceived)
            {
                TempData["Success"] =
                    $"Sample '{barcode}' was received successfully. " +
                    $"All samples for request #{request.RequestId} " +
                    $"have now been received. The laboratory tests are available.";
            }
            else
            {
                TempData["Success"] =
                    $"Sample '{barcode}' was received successfully. " +
                    $"Request #{request.RequestId} is partially received.";
            }

            return RedirectToAction(nameof(Receive));
        }

        // =========================================================
        // RECEIVED SAMPLES HISTORY
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