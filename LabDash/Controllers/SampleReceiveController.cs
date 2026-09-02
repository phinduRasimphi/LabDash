using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        // RECEIVE SAMPLE PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Receive()
        {
            await PopulateRequestList();

            return View(new SampleReceive());
        }

        // =========================================================
        // RECEIVE SAMPLE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(SampleReceive sample)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in technician.";

                return RedirectToAction(nameof(Receive));
            }

            // Validate request
            if (sample.RequestId <= 0)
            {
                TempData["Error"] =
                    "Please select a test request.";

                return RedirectToAction(nameof(Receive));
            }

            // Validate barcode
            if (string.IsNullOrWhiteSpace(sample.SampleBarcode))
            {
                TempData["Error"] =
                    "Please enter the sample barcode.";

                return RedirectToAction(nameof(Receive));
            }

            sample.SampleBarcode = sample.SampleBarcode.Trim();

            // =====================================================
            // LOAD REQUEST
            // =====================================================

            var request = await _context.TestRequests
                .Include(r => r.Samples)
                .Include(r => r.SampleReceives)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .FirstOrDefaultAsync(
                    r => r.RequestId == sample.RequestId);

            if (request == null)
            {
                TempData["Error"] =
                    $"Test request #{sample.RequestId} could not be found.";

                return RedirectToAction(nameof(Receive));
            }

            // =====================================================
            // CHECK REQUEST STATUS
            // =====================================================

            if (request.Status != "Pending" &&
                request.Status != "Partially Received")
            {
                TempData["Error"] =
                    $"Request #{request.RequestId} cannot receive samples. " +
                    $"Current status: {request.Status}";

                return RedirectToAction(nameof(Receive));
            }

            // =====================================================
            // FIND EXPECTED SAMPLE
            // =====================================================

            var expectedSample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.TestRequestId == sample.RequestId &&
                    s.Barcode == sample.SampleBarcode);

            if (expectedSample == null)
            {
                TempData["Error"] =
                    $"Barcode '{sample.SampleBarcode}' does not belong " +
                    $"to request #{sample.RequestId}.";

                return RedirectToAction(nameof(Receive));
            }

            // =====================================================
            // CHECK IF SAMPLE ALREADY RECEIVED
            // =====================================================

            if (expectedSample.IsReceived)
            {
                TempData["Error"] =
                    $"Sample '{sample.SampleBarcode}' has already been received.";

                return RedirectToAction(nameof(Receive));
            }

            var alreadyReceived = await _context.SampleReceives
                .AnyAsync(s =>
                    s.SampleBarcode == sample.SampleBarcode);

            if (alreadyReceived)
            {
                TempData["Error"] =
                    $"Sample barcode '{sample.SampleBarcode}' " +
                    $"has already been recorded.";

                return RedirectToAction(nameof(Receive));
            }

            // =====================================================
            // TECHNICIAN NAME
            // =====================================================

            string technicianName =
                !string.IsNullOrWhiteSpace(technician.UserName)
                    ? technician.UserName
                    : "Laboratory Technician";

            var now = DateTime.Now;

            // =====================================================
            // MARK SAMPLE AS RECEIVED
            // =====================================================

            expectedSample.IsReceived = true;
            expectedSample.DateReceived = now;
            expectedSample.ReceivedByTechnician = technicianName;

            // =====================================================
            // CREATE SAMPLE RECEIVE RECORD
            // =====================================================

            var sampleReceive = new SampleReceive
            {
                RequestId = request.RequestId,
                TechnicianName = technicianName,
                SampleBarcode = expectedSample.Barcode,
                SampleType = expectedSample.SampleType,
                DateTimeReceived = now,
                Status = "Samples Received",
                Notes = sample.Notes
            };

            _context.SampleReceives.Add(sampleReceive);

            // =====================================================
            // CHECK ALL SAMPLES FOR THIS REQUEST
            // =====================================================

            var allSamples = await _context.Samples
                .Where(s =>
                    s.TestRequestId == request.RequestId)
                .ToListAsync();

            bool allReceived =
                allSamples.Count > 0 &&
                allSamples.All(s => s.IsReceived);

            // =====================================================
            // UPDATE REQUEST STATUS
            // =====================================================

            if (allReceived)
            {
                request.Status = "Samples Received";
                request.DateTimeReceived = now;

                // =================================================
                // IMPORTANT:
                // MAKE TEST ITEMS AVAILABLE
                // =================================================

                foreach (var testItem in request.TestRequestItems)
                {
                    // Only make tests available if they have not
                    // already been started/completed.
                    if (string.IsNullOrWhiteSpace(testItem.Status) ||
                        testItem.Status == "Pending" ||
                        testItem.Status == "Requested" ||
                        testItem.Status == "Submitted")
                    {
                        testItem.Status = "Submitted";
                    }
                }
            }
            else
            {
                request.Status = "Partially Received";
            }

            // =====================================================
            // SAVE
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var errorMessage =
                    ex.InnerException?.Message ??
                    ex.Message;

                TempData["Error"] =
                    "The sample could not be saved. " +
                    "Database error: " + errorMessage;

                return RedirectToAction(nameof(Receive));
            }

            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                $"Sample '{expectedSample.Barcode}' received successfully " +
                $"for request #{request.RequestId}.";

            return RedirectToAction(
                "Index",
                "AvailableTests");
        }

        // =========================================================
        // AJAX - GET SAMPLE TYPE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetSampleType(
            int requestId,
            string barcode)
        {
            if (requestId <= 0)
            {
                return Json(new
                {
                    found = false,
                    message = "Select a test request."
                });
            }

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return Json(new
                {
                    found = false,
                    message = "Enter a barcode."
                });
            }

            barcode = barcode.Trim();

            var sample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.TestRequestId == requestId &&
                    s.Barcode == barcode);

            if (sample == null)
            {
                return Json(new
                {
                    found = false,
                    message =
                        "This barcode does not belong " +
                        "to the selected request."
                });
            }

            if (sample.IsReceived)
            {
                return Json(new
                {
                    found = false,
                    message =
                        "This sample has already been received."
                });
            }

            var alreadyReceived =
                await _context.SampleReceives
                    .AnyAsync(s =>
                        s.SampleBarcode == barcode);

            if (alreadyReceived)
            {
                return Json(new
                {
                    found = false,
                    message =
                        "This barcode has already been received."
                });
            }

            return Json(new
            {
                found = true,
                sampleType = sample.SampleType
            });
        }

        // =========================================================
        // RECEIVED SAMPLES LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var samples = await _context.SampleReceives
                .Include(s => s.TestRequest)
                    .ThenInclude(r => r.Patient)
                .OrderByDescending(
                    s => s.DateTimeReceived)
                .ToListAsync();

            return View(samples);
        }

        // =========================================================
        // POPULATE REQUEST DROPDOWN
        // =========================================================

        private async Task PopulateRequestList()
        {
            var requests = await _context.TestRequests
                .Where(r =>
                    r.Status == "Pending" ||
                    r.Status == "Partially Received")
                .OrderByDescending(r => r.RequestId)
                .ToListAsync();

            var requestItems =
                new List<SelectListItem>();

            foreach (var request in requests)
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.PatientID == request.PatientId);

                string patientName = "Patient";

                if (patient != null)
                {
                    patientName =
                        $"{patient.Name} {patient.Surname}".Trim();
                }

                requestItems.Add(new SelectListItem
                {
                    Value = request.RequestId.ToString(),
                    Text =
                        $"#{request.RequestId} — {patientName}"
                });
            }

            ViewBag.RequestList = requestItems;
            ViewBag.PendingRequestCount =
                requestItems.Count;
        }
    }
}