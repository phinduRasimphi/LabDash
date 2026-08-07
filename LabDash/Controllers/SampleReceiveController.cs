using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class SampleReceiveController : Controller
    {
        private readonly LabDbContext _context;

        public SampleReceiveController(LabDbContext context)
        {
            _context = context;
        }

        // GET: /SampleReceive/Receive
        [HttpGet]
        public IActionResult Receive()
        {
            PopulateRequestList();
            return View();
        }

        // POST: /SampleReceive/Receive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(SampleReceive sample)
        {
            // SampleType is now server-derived, not client-supplied — don't require
            // ModelState validity on it here; strip any client-side error for it
            // before checking ModelState so a blank hidden field doesn't block submission.
            ModelState.Remove(nameof(SampleReceive.SampleType));

            if (!ModelState.IsValid)
            {
                PopulateRequestList();
                return View(sample);
            }

            var request = await _context.TestRequests
                .Include(r => r.Samples)
                .Include(r => r.SampleReceives)
                .FirstOrDefaultAsync(r => r.RequestId == sample.RequestId);

            if (request == null)
            {
                ModelState.AddModelError("", "The selected test request could not be found.");
                PopulateRequestList();
                return View(sample);
            }

            // Allow "Pending" (first sample) or "Partially Received" (subsequent samples)
            if (request.Status != "Pending" && request.Status != "Partially Received")
            {
                ModelState.AddModelError("", "This request is not open to receive samples.");
                PopulateRequestList();
                return View(sample);
            }

            // Confirm the barcode matches a sample actually expected on this request
            // ASSUMPTION: Sample entity exposes "BarcodeNumber". Adjust if named differently.
            var expectedSample = request.Samples
                .FirstOrDefault(s => s.Barcode == sample.SampleBarcode);

            if (expectedSample == null)
            {
                ModelState.AddModelError("", "This barcode does not match any sample expected for the selected request.");
                PopulateRequestList();
                return View(sample);
            }

            // Prevent duplicate barcode submissions across the whole system
            bool barcodeAlreadyReceived = await _context.SampleReceives
                .AnyAsync(s => s.SampleBarcode == sample.SampleBarcode);

            if (barcodeAlreadyReceived)
            {
                ModelState.AddModelError("", "This barcode has already been recorded as received.");
                PopulateRequestList();
                return View(sample);
            }

            var now = DateTime.UtcNow;

            // Sample type comes from the matched expected sample — never trust the
            // client-posted value, since the form field is display-only.
            sample.SampleType = expectedSample.SampleType;
            sample.DateTimeReceived = now;
            sample.Status = "Samples Received";
            sample.TechnicianName = User.Identity?.Name ?? "Unknown";

            request.SampleReceives.Add(sample);

            // Determine whether ALL expected samples for this request are now received
            int expectedCount = request.Samples.Count;
            int receivedCount = request.SampleReceives.Count; // includes the one just added

            request.Status = receivedCount >= expectedCount
                ? "Samples Received"
                : "Partially Received";

            if (request.Status == "Samples Received")
            {
                request.DateTimeReceived = now;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "This request was updated by another user. Please refresh and try again.");
                PopulateRequestList();
                return View(sample);
            }

            TempData["Success"] = "Sample received successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /SampleReceive/GetSampleType?requestId=12&barcode=BC12345
        // Used by the Receive view to auto-resolve sample type from the scanned barcode.
        [HttpGet]
        public async Task<IActionResult> GetSampleType(int requestId, string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return Json(new { found = false, message = "Enter a barcode." });
            }

            var sample = await _context.Samples
                .FirstOrDefaultAsync(s => s.TestRequestId == requestId && s.Barcode == barcode);

            if (sample == null)
            {
                return Json(new { found = false, message = "No sample with this barcode is expected on the selected request." });
            }

            bool alreadyReceived = await _context.SampleReceives
                .AnyAsync(s => s.SampleBarcode == barcode);

            if (alreadyReceived)
            {
                return Json(new { found = false, message = "This barcode has already been received." });
            }

            return Json(new { found = true, sampleType = sample.SampleType });
        }

        // GET: /SampleReceive/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var samples = await _context.SampleReceives
                .Include(s => s.TestRequest)
                .OrderByDescending(s => s.DateTimeReceived)
                .ToListAsync();

            return View(samples);
        }

        private void PopulateRequestList()
        {
            var openRequests = _context.TestRequests
                .Where(r => r.Status == "Pending" || r.Status == "Partially Received")
                .Include(r => r.Patient)
                .Select(r => new
                {
                    r.RequestId,
                    // ASSUMPTION: Patient has a FullName property — adjust to match your model
                    Display = "#" + r.RequestId + " — " + r.Patient.Name + " (" + r.RequestDate.ToString("dd MMM") + ")"
                })
                .ToList();

            ViewBag.RequestList = new SelectList(openRequests, "RequestId", "Display");
        }
    }
}