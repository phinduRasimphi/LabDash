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
        // GET: /SampleReceive/Receive
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Receive()
        {
            await PopulateRequestList();

            return View(new SampleReceive());
        }

        // =========================================================
        // POST: /SampleReceive/Receive
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(SampleReceive sample)
        {
            // -----------------------------------------------------
            // GET LOGGED-IN TECHNICIAN
            // -----------------------------------------------------

            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] = "Unable to identify the logged-in technician.";
                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // VALIDATE REQUEST NUMBER
            // -----------------------------------------------------

            if (sample.RequestId <= 0)
            {
                TempData["Error"] = "Please select a test request.";
                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // VALIDATE BARCODE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(sample.SampleBarcode))
            {
                TempData["Error"] = "Please enter the sample barcode.";
                return RedirectToAction(nameof(Receive));
            }

            sample.SampleBarcode = sample.SampleBarcode.Trim();

            // -----------------------------------------------------
            // FIND TEST REQUEST
            // -----------------------------------------------------

            var request = await _context.TestRequests
                .Include(r => r.Samples)
                .Include(r => r.SampleReceives)
                .Include(r => r.TestRequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == sample.RequestId);

            if (request == null)
            {
                TempData["Error"] =
                    $"Test request #{sample.RequestId} could not be found.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK REQUEST STATUS
            // -----------------------------------------------------

            if (request.Status != "Pending" &&
                request.Status != "Partially Received")
            {
                TempData["Error"] =
                    $"Request #{request.RequestId} cannot receive samples because its current status is '{request.Status}'.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // FIND SAMPLE USING BARCODE
            // -----------------------------------------------------

            var expectedSample = await _context.Samples
                .FirstOrDefaultAsync(s =>
                    s.TestRequestId == sample.RequestId &&
                    s.Barcode == sample.SampleBarcode);

            if (expectedSample == null)
            {
                TempData["Error"] =
                    $"Barcode '{sample.SampleBarcode}' does not belong to request #{sample.RequestId}.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CHECK WHETHER THIS SAMPLE WAS ALREADY RECEIVED
            // -----------------------------------------------------

            if (expectedSample.IsReceived)
            {
                TempData["Error"] =
                    $"Sample barcode '{sample.SampleBarcode}' has already been received.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // ALSO CHECK SAMPLE RECEIVES TABLE
            // -----------------------------------------------------

            var alreadyReceived = await _context.SampleReceives
                .AnyAsync(s =>
                    s.SampleBarcode == sample.SampleBarcode);

            if (alreadyReceived)
            {
                TempData["Error"] =
                    $"Sample barcode '{sample.SampleBarcode}' has already been recorded.";

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // CURRENT DATE/TIME
            // -----------------------------------------------------

            var now = DateTime.Now;

            // -----------------------------------------------------
            // TECHNICIAN NAME
            //
            // Your database requires TechnicianName.
            //
            // We use the logged-in user's name.
            // -----------------------------------------------------

            string technicianName;

            if (!string.IsNullOrWhiteSpace(technician.UserName))
            {
                technicianName = technician.UserName;
            }
            else
            {
                technicianName = "Laboratory Technician";
            }

            // -----------------------------------------------------
            // UPDATE THE ORIGINAL SAMPLE
            //
            // THIS IS VERY IMPORTANT.
            //
            // Your Samples table contains:
            //
            // IsReceived
            // DateReceived
            // ReceivedByTechnician
            //
            // These must be updated.
            // -----------------------------------------------------

            expectedSample.IsReceived = true;

            expectedSample.DateReceived = now;

            expectedSample.ReceivedByTechnician = technicianName;

            // -----------------------------------------------------
            // CREATE SAMPLE RECEIVE RECORD
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // ADD SAMPLE RECEIVE
            // -----------------------------------------------------

            _context.SampleReceives.Add(sampleReceive);

            // -----------------------------------------------------
            // CHECK WHETHER ALL SAMPLES FOR THIS REQUEST
            // HAVE NOW BEEN RECEIVED
            // -----------------------------------------------------

            var allSamples = await _context.Samples
                .Where(s => s.TestRequestId == request.RequestId)
                .ToListAsync();

            bool allReceived =
                allSamples.Count > 0 &&
                allSamples.All(s => s.IsReceived);

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
            catch (Exception ex)
            {
                TempData["Error"] =
                    "The sample could not be saved. Database error: " +
                    ex.InnerException?.Message ??
                    ex.Message;

                return RedirectToAction(nameof(Receive));
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["Success"] =
                $"Sample '{expectedSample.Barcode}' received successfully for request #{request.RequestId}.";

            // -----------------------------------------------------
            // GO DIRECTLY TO AVAILABLE TESTS
            // -----------------------------------------------------

            return RedirectToAction(
                "Index",
                "AvailableTests");
        }

        // =========================================================
        // GET SAMPLE TYPE
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
                        "This barcode does not belong to the selected request."
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
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var samples = await _context.SampleReceives

                .Include(s => s.TestRequest)

                .OrderByDescending(s => s.DateTimeReceived)

                .ToListAsync();

            return View(samples);
        }

        // =========================================================
        // REQUEST LIST
        // =========================================================

        private async Task PopulateRequestList()
        {
            var openRequests = await _context.TestRequests

                .Where(r =>
                    r.Status == "Pending" ||
                    r.Status == "Partially Received")

                .Include(r => r.Patient)

                .OrderByDescending(r => r.RequestDate)

                .Select(r => new
                {
                    r.RequestId,

                    Display =
                        "#" +
                        r.RequestId +
                        " — " +
                        (
                            r.Patient != null
                                ? r.Patient.Name +
                                  " " +
                                  r.Patient.Surname
                                : "Patient"
                        ) +
                        " (" +
                        r.RequestDate.ToString("dd MMM yyyy") +
                        ")"
                })

                .ToListAsync();

            ViewBag.RequestList =
                new SelectList(
                    openRequests,
                    "RequestId",
                    "Display");
        }
    }
}