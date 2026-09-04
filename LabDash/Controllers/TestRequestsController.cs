using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class TestRequestController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IEmailSender _emailSender;

        public TestRequestController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: /TestRequest/Index
        public async Task<IActionResult> Index()
        {
            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
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

        // POST: /TestRequest/UnlockFolder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFolder(int patientId, string idNumber)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (patient.IDNumber != idNumber?.Trim())
            {
                TempData["Error"] = "Incorrect ID number. Folder remains locked.";
                return RedirectToAction(nameof(Index));
            }

            var requests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

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
        // POST: /TestRequest/Create
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
            // =========================================================
            // 1. VALIDATE SELECTED TESTS
            // =========================================================

            if (selectedTestTypeIds == null ||
                selectedTestTypeIds.Length == 0)
            {
                TempData["Error"] =
                    "Please select at least one test type.";

                return RedirectToAction(nameof(Create),
                    new { patientId });
            }

            if (string.IsNullOrWhiteSpace(sampleBarcode1) && string.IsNullOrWhiteSpace(sampleBarcode2))
            {
                TempData["Error"] = "At least one sample barcode is required.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            // Remove duplicate test IDs
            selectedTestTypeIds = selectedTestTypeIds
                .Distinct()
                .ToArray();


            // =========================================================
            // 2. GET LOGGED-IN DOCTOR
            // =========================================================

            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in doctor.";

                return RedirectToAction(nameof(Create),
                    new { patientId });
            }


            // =========================================================
            // 3. GET PATIENT
            // =========================================================

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.PatientID == patientId);

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient not found.";

                return RedirectToAction(
                    "ManagePatients",
                    "Doctor");
            }


            // =========================================================
            // 4. GET SELECTED TEST TYPES
            // =========================================================

            var selectedTypes = await _context.TestTypes
                .Where(t => selectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            if (selectedTypes.Count != selectedTestTypeIds.Length)
            {
                TempData["Error"] =
                    "One or more selected laboratory tests could not be found. " +
                    "Please refresh the page and select the tests again.";

                return RedirectToAction(nameof(Create),
                    new { patientId });
            }


            // =========================================================
            // 5. COLLECT SAMPLE BARCODES
            // =========================================================

            var barcodes = new List<string>();

            if (!string.IsNullOrWhiteSpace(sampleBarcode1))
            {
                barcodes.Add(sampleBarcode1.Trim());
            }

            if (!string.IsNullOrWhiteSpace(sampleBarcode2))
            {
                barcodes.Add(sampleBarcode2.Trim());
            }


            // =========================================================
            // 6. CREATE TEST REQUEST
            // =========================================================

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

            // IMPORTANT:
            // Save the request first so that RequestId is generated.
            await _context.SaveChangesAsync();


            // =========================================================
            // 7. CREATE TEST REQUEST ITEMS
            // =========================================================

            foreach (var testType in selectedTypes)
            {
                var testItem = new TestRequestItem
                {
                    RequestId = testRequest.RequestId,
                    TestTypeId = testType.Id,
                    Status = "Submitted",
                    AssignedTechnicianId = null,
                    StartDateTime = null,
                    CompletionDateTime = null
                };

                _context.TestRequestItems.Add(testItem);
            }


            // =========================================================
            // 8. SAVE TEST REQUEST ITEMS
            // =========================================================

            await _context.SaveChangesAsync();

            // ===== SEND EMAIL TO PATIENT =====
            string testListHtml = string.Join(", ", selectedTypes.Select(t => t.Name));
            string emailBody = $@"
                <p>Dear {patient.Name},</p>
                <p>Dr. {doctor.LastName} has submitted a test request for you at NMB LAB.</p>
                <p><strong>Tests requested:</strong> {testListHtml}<br/>
                <strong>Date:</strong> {testRequest.RequestDate:dd MMM yyyy}</p>
                <p>Please visit the lab at your earliest convenience to provide the required samples.</p>";

            await _emailSender.SendEmailAsync(patient.Email, "New Test Request Submitted", emailBody);

            TempData["SuccessMessage"] = "Test request successfully sent to the lab!";

            // =========================================================
            // 9. VERIFY THAT ITEMS WERE CREATED
            // =========================================================

            var itemCount = await _context.TestRequestItems
                .CountAsync(x =>
                    x.RequestId == testRequest.RequestId);

            if (itemCount == 0)
            {
                TempData["Error"] =
                    $"Request #{testRequest.RequestId} was created, " +
                    "but no laboratory test items were created.";

                return RedirectToAction(nameof(Index));
            }


            // =========================================================
            // 10. SUCCESS
            // =========================================================

            TempData["SuccessMessage"] =
                $"Test request #{testRequest.RequestId} created successfully " +
                $"with {itemCount} laboratory test(s).";

            return RedirectToAction(nameof(Index));
        }

        // GET: /TestRequest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .FirstOrDefaultAsync(tr => tr.RequestId == id);

            if (request == null) return NotFound();

            ViewBag.RequestId = request.RequestId;
            ViewBag.PatientName = $"{request.Patient.Name} {request.Patient.Surname}";
            ViewBag.PatientIDNumber = request.Patient.IDNumber;

            var barcodes = string.IsNullOrEmpty(request.SampleBarcodes)
                ? new List<string>()
                : request.SampleBarcodes.Split(',').ToList();

            ViewBag.Barcode1 = barcodes.Count > 0 ? barcodes[0] : "";
            ViewBag.Barcode2 = barcodes.Count > 1 ? barcodes[1] : "";

            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Category).ThenBy(t => t.Name)
                .ToListAsync();
            ViewBag.TestTypes = testTypes;

            ViewBag.SelectedTestTypeIds = request.TestRequestItems.Select(i => i.TestTypeId).ToList();

            return View();
        }

        // POST: /TestRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int requestId,
            DateTime requestDate,
            string urgency,
            string? clinicalNotes,
            int[] selectedTestTypeIds,
            string? sampleBarcode1,
            string? sampleBarcode2)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.TestRequestItems)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();

            if (selectedTestTypeIds == null || !selectedTestTypeIds.Any())
            {
                TempData["Error"] = "Select at least one test type.";
                return RedirectToAction(nameof(Edit), new { id = requestId });
            }

            request.RequestDate = requestDate;
            request.Urgency = urgency;
            request.ClinicalNotes = clinicalNotes;
            request.SampleBarcodes = string.Join(",", new[] { sampleBarcode1, sampleBarcode2 }
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray());

            _context.TestRequestItems.RemoveRange(request.TestRequestItems);
            await _context.SaveChangesAsync();

            var selectedTypes = await _context.TestTypes
                .Where(t => selectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            foreach (var type in selectedTypes)
            {
                _context.TestRequestItems.Add(new TestRequestItem
                {
                    RequestId = request.RequestId,
                    TestTypeId = type.Id,
                    Status = "Submitted"
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test request updated successfully!";
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
        // GET: /TestRequest/Track
        public async Task<IActionResult> Track()
        {
            var doctor = await _userManager.GetUserAsync(User);

            // Fetch all requests for this doctor
            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .ToListAsync();

            // Group by patient
            var folders = allRequests
                .GroupBy(tr => tr.PatientId)
                .Select(g => new TrackRequestViewModel
                {
                    Patient = g.First().Patient,
                    Requests = g.OrderByDescending(r => r.RequestDate).ToList()
                })
                .OrderBy(x => x.Patient.Name)
                .ToList();

            return View(folders);
        }
        // GET: TestRequest/Index
        public async Task<IActionResult> Index()
        {
            var testRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .OrderByDescending(tr => tr.RequestDate)
                .ToListAsync();

            var viewModel = testRequests.Select(tr => new TestRequestListViewModel
            {
                RequestId = tr.RequestId,
                PatientName = tr.Patient != null ? $"{tr.Patient.Name} {tr.Patient.Surname}" : "Unknown",
                DoctorName = tr.RequestingDoctor != null ? tr.RequestingDoctor.FullName : "Unknown",
                RequestDate = tr.RequestDate,
                Urgency = tr.Urgency,
                Status = tr.Status,
                HasAbnormalResults = false,
                ResultCount = tr.TestRequestItems?.Count ?? 0,

                // ===== TEST TYPES =====
                TestTypeNames = tr.TestRequestItems?.Select(tri => tri.TestType?.Name ?? "Unknown").ToList() ?? new List<string>(),
                TestTypesDisplay = tr.TestRequestItems != null && tr.TestRequestItems.Any()
                    ? string.Join(", ", tr.TestRequestItems.Select(tri => tri.TestType?.Name ?? "Unknown"))
                    : "No tests",

                // ===== BARCODES =====
                SampleBarcodes = !string.IsNullOrEmpty(tr.SampleBarcodes)
                    ? tr.SampleBarcodes.Split(',').ToList()
                    : new List<string>(),
                SampleBarcodesString = tr.SampleBarcodes ?? "",

                // ===== CANCELLATION REASON =====
                CancellationReason = tr.CancellationReason,

                // ===== CLINICAL NOTES =====
                ClinicalNotes = tr.ClinicalNotes
            });

            return View(viewModel);
        }
        // POST: /TestRequest/ReleaseResults
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

            request.Status = "Released by doctor";
            request.ReleaseNote = releaseNote;
            request.ReleaseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Results released to patient.";
            return RedirectToAction(nameof(Track)); 
        }
        // GET: /TestRequest/Results
        public async Task<IActionResult> Results()
        {
            var doctor = await _userManager.GetUserAsync(User);

            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id && (r.Status == "Completed" || r.Status == "Released by doctor"))
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
        // GET: /TestRequest/ViewRequest/5
        public async Task<IActionResult> ViewRequest(int id)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .FirstOrDefaultAsync(tr => tr.RequestId == id);

            if (request == null) return NotFound();

            // Pass it as a list so it uses the exact same PatientRequests.cshtml view
            return View("PatientRequests", new List<TestRequest> { request });
        }
    }
 }