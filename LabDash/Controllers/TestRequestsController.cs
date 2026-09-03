using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class TestRequestController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public TestRequestController(LabDbContext context, UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /TestRequest/Index
        public async Task<IActionResult> Index()
        {
            // 1. Fetch all requests with related data
            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .ToListAsync();

            // 2. Group them in memory (avoids EF Core GroupBy bug)
            var folders = allRequests
                .GroupBy(tr => tr.PatientId)
                .Select(g => new
                {
                    Patient = g.First().Patient,
                    Requests = g.OrderByDescending(r => r.RequestDate).ToList()
                })
                .OrderBy(x => x.Patient.Name)
                .ToList();

            return View(folders);
        }

        // POST: /TestRequest/UnlockFolder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFolder(int patientId, string idNumber)
        {
            // Check if ID matches
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (patient.IDNumber != idNumber?.Trim())
            {
                TempData["Error"] = "Incorrect ID number. Folder remains locked.";
                return RedirectToAction(nameof(Index));
            }

            // Get the specific requests for this patient
            var requests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            // Return a different view that shows the unlocked folder
            return View("PatientRequests", requests);
        }

        // GET: /TestRequest/Create?patientId=5
        public async Task<IActionResult> Create(int? patientId)
        {
            if (patientId == null)
            {
                TempData["Error"] = "No patient selected. Please search for a patient first.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            ViewBag.PatientId = patient.PatientID;
            ViewBag.PatientName = $"{patient.Name} {patient.Surname}";
            ViewBag.PatientIDNumber = patient.IDNumber;

            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Category).ThenBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes = testTypes;

            return View();
        }

        // POST: /TestRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int patientId,
            DateTime requestDate,
            string urgency,
            string? clinicalNotes,
            int[] selectedTestTypeIds,
            string? sampleBarcode1,
            string? sampleBarcode2)
        {
            if (selectedTestTypeIds == null || !selectedTestTypeIds.Any())
            {
                TempData["Error"] = "Select at least one test type.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            var doctor = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var barcodes = new[] { sampleBarcode1, sampleBarcode2 }
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray();

            var testRequest = new TestRequest
            {
                PatientId = patientId,
                RequestingDoctorId = doctor.Id,
                RequestDate = requestDate,
                Urgency = urgency,
                ClinicalNotes = clinicalNotes,
                Status = "Submitted",
                SubmittedDate = DateTime.Now,
                SampleBarcodes = string.Join(",", barcodes)
            };

            _context.TestRequests.Add(testRequest);
            await _context.SaveChangesAsync();

            var selectedTypes = await _context.TestTypes
                .Where(t => selectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            foreach (var type in selectedTypes)
            {
                _context.TestRequestItems.Add(new TestRequestItem
                {
                    RequestId = testRequest.RequestId,
                    TestTypeId = type.Id,
                    Status = "Submitted"
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test request successfully sent to the lab!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /TestRequest/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId, string cancellationReason)
        {
            var doctor = await _userManager.GetUserAsync(User);

            var request = await _context.TestRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.Status != "Submitted" && request.Status != "Samples Received")
            {
                TempData["Error"] = "This request can no longer be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] = "A cancellation reason is required.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Cancelled";
            request.CancellationReason = cancellationReason;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Test request cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /TestRequest/Results
        public async Task<IActionResult> Results()
        {
            var doctor = await _userManager.GetUserAsync(User);

            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id && (r.Status == "Completed" || r.Status == "Released"))
                .ToListAsync();

            var folders = allRequests
                .GroupBy(tr => tr.PatientId)
                .Select(g => new
                {
                    Patient = g.First().Patient,
                    Requests = g.OrderByDescending(r => r.RequestDate).ToList()
                })
                .OrderBy(x => x.Patient.Name)
                .ToList();

            return View(folders);
        }

        // POST: /TestRequest/ReleaseResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResults(int requestId, string releaseNote)
        {
            var doctor = await _userManager.GetUserAsync(User);

            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null) return NotFound();
            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.Status != "Completed")
            {
                TempData["Error"] = "Only completed results can be released.";
                return RedirectToAction(nameof(Results));
            }

            request.Status = "Released";
            request.ReleaseNote = releaseNote;
            request.ReleaseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Results released to patient.";
            return RedirectToAction(nameof(Results));
        }
    }
}