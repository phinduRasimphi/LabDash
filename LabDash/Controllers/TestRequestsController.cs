using LabDash.Areas.Identity.Data;
using LabDash.Helpers;
using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using LabDash.ViewModels;
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

        // =============================================================
        // INDEX — folders grouped by patient
        // =============================================================
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

        // =============================================================
        // UNLOCK FOLDER — verify ID number, then show requests
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFolder(int patientId, string idNumber)
        {
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient == null)
                return NotFound();

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

        // =============================================================
        // CREATE — GET
        // =============================================================
        // CREATE — GET
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Create(int? patientId)
        {
            if (patientId == null)
            {
                TempData["Error"] =
                    "No patient selected. Please search for a patient first.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == patientId.Value);

            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            ViewBag.PatientId = patient.PatientID;
            ViewBag.PatientName = $"{patient.Name} {patient.Surname}";
            ViewBag.PatientIDNumber = patient.IDNumber;
            ViewBag.PatientDOB = patient.DOB.ToString("dd MMM yyyy");
            ViewBag.PatientEmail = patient.Email ?? "";
            ViewBag.PatientCell = patient.CellphoneNumber ?? "";
            ViewBag.RequestDate = DateTime.Today;

            // =========================================================
            // 1. ALLERGIES (Junction first, Fallback to string)
            // =========================================================
            var allergies = await _context.PatientAllergies
                .AsNoTracking()
                .Where(a => a.PatientID == patientId.Value)
                .Include(a => a.Allergy)
                    .ThenInclude(al => al!.Category)
                .Select(a => new
                {
                    Name = a.Allergy != null ? a.Allergy.AllergyName : "Unknown",
                    Category = a.Allergy != null && a.Allergy.Category != null
                                ? a.Allergy.Category.Name
                                : "Uncategorised",
                    Severity = a.Severity ?? ""
                })
                .ToListAsync();

            if (allergies.Count == 0 &&
                !string.IsNullOrWhiteSpace(patient.Allergies) &&
                !patient.Allergies.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                allergies = patient.Allergies
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Select(x => new
                    {
                        Name = x,
                        Category = "Uncategorised",
                        Severity = ""
                    })
                    .ToList();
            }
            ViewBag.PatientAllergies = allergies;

            // =========================================================
            // 2. CONDITIONS (Junction first, Fallback to string)
            // =========================================================
            var conditions = await _context.PatientMedicalConditions
                .AsNoTracking()
                .Where(c => c.PatientID == patientId.Value)
                .Include(c => c.MedicalCondition)
                    .ThenInclude(mc => mc!.Category)
                .Select(c => new
                {
                    Name = c.MedicalCondition != null
                            ? c.MedicalCondition.ConditionName
                            : "Unknown",
                    Category = c.MedicalCondition != null &&
                               c.MedicalCondition.Category != null
                                ? c.MedicalCondition.Category.Name
                                : "Uncategorised",
                    Severity = c.Severity ?? ""
                })
                .ToListAsync();

            if (conditions.Count == 0 &&
                !string.IsNullOrWhiteSpace(patient.MedicalConditions) &&
                !patient.MedicalConditions.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                conditions = patient.MedicalConditions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Select(x => new
                    {
                        Name = x,
                        Category = "Uncategorised",
                        Severity = ""
                    })
                    .ToList();
            }
            ViewBag.PatientConditions = conditions;

            // =========================================================
            // 3. MEDICATIONS (Junction first, Fallback to string)
            // =========================================================
            var medications = await _context.PatientMedications
                .AsNoTracking()
                .Where(m => m.PatientID == patientId.Value)
                .Include(m => m.Medication)
                    .ThenInclude(med => med!.Category)
                .Select(m => new
                {
                    Name = m.Medication != null
                            ? m.Medication.MedicationName
                            : "Unknown",
                    Category = m.Medication != null &&
                               m.Medication.Category != null
                                ? m.Medication.Category.Name
                                : "Uncategorised",
                    Dosage = m.Dosage ?? "",
                    Frequency = m.Frequency ?? ""
                })
                .ToListAsync();

            if (medications.Count == 0 &&
                !string.IsNullOrWhiteSpace(patient.Medication) &&
                !patient.Medication.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                medications = patient.Medication
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Select(x => new
                    {
                        Name = x,
                        Category = "Uncategorised",
                        Dosage = "",
                        Frequency = ""
                    })
                    .ToList();
            }
            ViewBag.PatientMedications = medications;

            // =========================================================
            // 4. PREVIOUS RESULTS
            // =========================================================
            var previousResults = await _context.TestResults
                .AsNoTracking()
                .Where(r =>
                    r.TestRequestItem != null &&
                    r.TestRequestItem.TestRequest != null &&
                    r.TestRequestItem.TestRequest.PatientId == patientId.Value)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .OrderByDescending(r => r.DateCaptured)
                .Take(20)
                .Select(r => new
                {
                    TestName = r.TestRequestItem.TestType != null
                                ? r.TestRequestItem.TestType.Name
                                : "Unknown",
                    ResultValue = r.ResultValue,
                    Units = r.Units,
                    IsAbnormal = r.IsAbnormal,
                    DateCaptured = r.DateCaptured,
                    Status = r.Status
                })
                .ToListAsync();

            ViewBag.PreviousResults = previousResults;

            // =========================================================
            // 5. TEST TYPES
            // =========================================================
            var testTypes = await _context.TestTypes
                .AsNoTracking()
                .Include(t => t.TestCategory)
                .OrderBy(t => t.TestCategory != null
                                ? t.TestCategory.CategoryName
                                : t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes = testTypes;

            return View();
        }
        

        // =============================================================
        // CREATE — POST
        // FIX: Now creates Sample entities for each barcode.
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int patientId,
            string urgency,
            string? clinicalNotes,
            int[]? selectedTestTypeIds,
            string[]? barcodes)
        {
            // 1. Validate test selection
            if (selectedTestTypeIds == null || selectedTestTypeIds.Length == 0)
            {
                TempData["Error"] = "Please select at least one test type.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            // 2. Validate barcodes
            var cleanBarcodes = (barcodes ?? Array.Empty<string>())
                .Select(b => (b ?? "").Trim())
                .ToArray();

            if (cleanBarcodes.Length != selectedTestTypeIds.Length ||
                cleanBarcodes.Any(b => string.IsNullOrWhiteSpace(b)))
            {
                TempData["Error"] =
                    "Each selected test must have its own barcode.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            if (cleanBarcodes
                    .Select(b => b.ToLowerInvariant())
                    .Distinct()
                    .Count() != cleanBarcodes.Length)
            {
                TempData["Error"] = "Each barcode must be unique.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            // 3. Identify the doctor
            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null)
            {
                TempData["Error"] = "Unable to identify the logged-in doctor.";
                return RedirectToAction(nameof(Create), new { patientId });
            }

            // 4. Get the patient
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            // 5. Load selected test types
            var selectedTypes = await _context.TestTypes
                .Where(t => selectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            if (selectedTypes.Count != selectedTestTypeIds.Length)
            {
                TempData["Error"] =
                    "One or more selected laboratory tests could not be found. " +
                    "Please refresh the page and select the tests again.";

                return RedirectToAction(nameof(Create), new { patientId });
            }

            // 6. Create the test request
            var testRequest = new TestRequest
            {
                PatientId = patientId,
                RequestingDoctorId = doctor.Id,
                RequestDate = DateTime.Today,
                Urgency = urgency,
                ClinicalNotes = clinicalNotes,
                Status = "Submitted",
                SubmittedDate = DateTime.Now,
                SampleBarcodes = string.Join(",", cleanBarcodes)
            };

            _context.TestRequests.Add(testRequest);
            await _context.SaveChangesAsync();

            // 7. Create TestRequestItems — one per selected test
            for (int i = 0; i < selectedTestTypeIds.Length; i++)
            {
                int typeId = selectedTestTypeIds[i];

                _context.TestRequestItems.Add(new TestRequestItem
                {
                    RequestId = testRequest.RequestId,
                    TestTypeId = typeId,
                    Status = "Submitted",
                    AssignedTechnicianId = null,
                    StartDateTime = null,
                    CompletionDateTime = null
                });
            }

            await _context.SaveChangesAsync();

            // =========================================================
            // 7b. CREATE SAMPLE ENTITIES — THIS IS THE FIX
            // One Sample per barcode, so the technician can receive them.
            // =========================================================
            for (int i = 0; i < cleanBarcodes.Length; i++)
            {
                int typeId = selectedTestTypeIds[i];
                var testType = selectedTypes.FirstOrDefault(t => t.Id == typeId);

                _context.Samples.Add(new Sample
                {
                    TestRequestId = testRequest.RequestId,
                    Barcode = cleanBarcodes[i],
                    SampleType = testType?.RequiredSampleType ?? "Whole Blood",
                    IsReceived = false,
                    DateReceived = null,
                    ReceivedByTechnician = ""
                });
            }

            await _context.SaveChangesAsync();

            // 8. Notify the patient by email
            string testListHtml = string.Join(", ",
                selectedTypes.Select(t => t.Name));

            string emailBody = $@"
                <p>Dear {patient.Name},</p>
                <p>Dr. {doctor.LastName} has submitted a test request for you at NMB LAB.</p>
                <p>
                    <strong>Tests requested:</strong> {testListHtml}<br/>
                    <strong>Date:</strong> {testRequest.RequestDate:dd MMM yyyy}
                </p>
                <p>
                    Please visit the lab at your earliest convenience
                    to provide the required samples.
                </p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    patient.Email,
                    "New Test Request Submitted",
                    emailBody);
            }
            catch
            {
                // Request is saved regardless of email success.
            }

            // 9. Confirm everything was created
            var itemCount = await _context.TestRequestItems
                .CountAsync(x => x.RequestId == testRequest.RequestId);

            var sampleCount = await _context.Samples
                .CountAsync(x => x.TestRequestId == testRequest.RequestId);

            if (itemCount == 0)
            {
                TempData["Error"] =
                    $"Request #{testRequest.RequestId} was created, " +
                    "but no laboratory test items were created.";

                return RedirectToAction(nameof(Index));
            }

            // 10. Success
            TempData["SuccessMessage"] =
                $"Test request #{testRequest.RequestId} created successfully " +
                $"with {itemCount} test(s) and {sampleCount} sample(s).";

            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // EDIT — GET
        // =============================================================
        [HttpGet]
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
            ViewBag.PatientName =
                $"{request.Patient.Name} {request.Patient.Surname}";
            ViewBag.PatientIDNumber = request.Patient.IDNumber;
            ViewBag.RequestDate = request.RequestDate;
            ViewBag.Urgency = request.Urgency;
            ViewBag.ClinicalNotes = request.ClinicalNotes ?? "";

            var existingBarcodes = string.IsNullOrEmpty(request.SampleBarcodes)
                ? new List<string>()
                : request.SampleBarcodes.Split(',').ToList();

            ViewBag.Barcodes = existingBarcodes;

            var testTypes = await _context.TestTypes
                .Include(t => t.TestCategory)
                .OrderBy(t => t.TestCategory != null
                                ? t.TestCategory.CategoryName
                                : t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes = testTypes;

            ViewBag.SelectedTestTypeIds = request.TestRequestItems
                .Select(i => i.TestTypeId)
                .ToList();

            return View();
        }

        // =============================================================
        // EDIT — POST
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int requestId,
            string urgency,
            string? clinicalNotes,
            int[]? selectedTestTypeIds,
            string[]? barcodes)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.TestRequestItems)
                .Include(tr => tr.Samples)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();

            if (selectedTestTypeIds == null || selectedTestTypeIds.Length == 0)
            {
                TempData["Error"] = "Select at least one test type.";
                return RedirectToAction(nameof(Edit), new { id = requestId });
            }

            var cleanBarcodes = (barcodes ?? Array.Empty<string>())
                .Select(b => (b ?? "").Trim())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray();

            if (cleanBarcodes.Length != selectedTestTypeIds.Length)
            {
                TempData["Error"] =
                    "Each selected test must have its own barcode.";
                return RedirectToAction(nameof(Edit), new { id = requestId });
            }

            request.Urgency = urgency;
            request.ClinicalNotes = clinicalNotes;
            request.SampleBarcodes = string.Join(",", cleanBarcodes);

            _context.TestRequestItems.RemoveRange(request.TestRequestItems);
            await _context.SaveChangesAsync();

            var selectedTypes = await _context.TestTypes
                .Where(t => selectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            // Recreate items
            foreach (var type in selectedTypes)
            {
                _context.TestRequestItems.Add(new TestRequestItem
                {
                    RequestId = request.RequestId,
                    TestTypeId = type.Id,
                    Status = "Submitted"
                });
            }

            // Recreate samples for the new barcodes
            _context.Samples.RemoveRange(request.Samples);

            for (int i = 0; i < cleanBarcodes.Length; i++)
            {
                int typeId = selectedTestTypeIds[i];
                var testType = selectedTypes.FirstOrDefault(t => t.Id == typeId);

                _context.Samples.Add(new Sample
                {
                    TestRequestId = request.RequestId,
                    Barcode = cleanBarcodes[i],
                    SampleType = testType?.RequiredSampleType ?? "Whole Blood",
                    IsReceived = false,
                    DateReceived = null,
                    ReceivedByTechnician = ""
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test request updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // CANCEL
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId, string cancellationReason)
        {
            var doctor = await _userManager.GetUserAsync(User);

            var request = await _context.TestRequests.FindAsync(requestId);

            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.Status != "Submitted" &&
                request.Status != "Samples Received")
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

        // =============================================================
        // TRACK
        // =============================================================
        public async Task<IActionResult> Track()
        {
            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null) return Forbid();

            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var folders = allRequests
                .GroupBy(tr => tr.PatientId)
                .Select(g => new TrackRequestViewModel
                {
                    Patient = g.First().Patient,
                    Requests = g.OrderByDescending(r => r.RequestDate).ToList()
                })
                .OrderBy(x => x.Patient.Name)
                .ToList();

            ViewBag.TotalRequests = allRequests.Count;
            ViewBag.StatusSubmitted = allRequests.Count(r => r.Status == "Submitted");
            ViewBag.StatusReceived = allRequests.Count(r => r.Status == "Samples Received");
            ViewBag.StatusInProgress = allRequests.Count(r => r.Status == "In Progress");
            ViewBag.StatusCompleted = allRequests.Count(r => r.Status == "Completed");
            ViewBag.StatusReleased = allRequests.Count(r => r.Status == "Released by doctor");
            ViewBag.StatusCancelled = allRequests.Count(r => r.Status == "Cancelled");

            return View(folders);
        }

        // =============================================================
        // RELEASE REQUEST
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseRequest(int requestId)
        {
            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(tr => tr.TestRequestItems)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.TestRequestItems == null ||
                !request.TestRequestItems.Any() ||
                request.TestRequestItems.Any(i => i.Status != "Completed"))
            {
                TempData["Error"] = "All tests must be completed before releasing.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Released by doctor";
            request.ReleaseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Results released to the patient.";
            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // VIEW REQUEST
        // =============================================================
        public async Task<IActionResult> ViewRequest(int id)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .FirstOrDefaultAsync(tr => tr.RequestId == id);

            if (request == null) return NotFound();

            return View("PatientRequests", new List<TestRequest> { request });
        }

        // =============================================================
        // PATIENT RESULTS
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> PatientResults(int requestId)
        {
            // 🆕 Password gate — must have unlocked this session
            if (HttpContext.Session.GetString("DoctorResultsAccess") != "Granted")
            {
                return RedirectToAction(nameof(ResultsAccess), new { requestId });
            }

            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestResults)
                        .ThenInclude(r => r.CapturedByTechnician)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            bool canView =
                request.Status == "Completed" ||
                request.Status == "Released by doctor";

            if (!canView)
            {
                TempData["Error"] = "Results are not yet available for this request.";
                return RedirectToAction(nameof(ViewRequest), new { id = requestId });
            }

            return View("Results", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseWithNote(
    int requestId,
    string mode,
    string? note)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestResults)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();
            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.TestRequestItems == null ||
                !request.TestRequestItems.Any() ||
                request.TestRequestItems.Any(i => i.Status != "Completed"))
            {
                TempData["Error"] = "All tests must be completed before releasing.";
                return RedirectToAction(nameof(PatientResults), new { requestId });
            }

            var patient = request.Patient;
            if (patient == null || string.IsNullOrWhiteSpace(patient.Email))
            {
                TempData["Error"] = "Patient email is missing. Cannot notify.";
                return RedirectToAction(nameof(PatientResults), new { requestId });
            }

            // Build email using the helper class
            string subject = mode == "InviteConsultation"
                ? "Please book an appointment to discuss your results"
                : "Your Laboratory Results";

            string body = mode == "InviteConsultation"
                ? LabDash.Helpers.ResultEmailBuilder.BuildConsultationEmail(request, doctor, note)
                : LabDash.Helpers.ResultEmailBuilder.BuildResultsEmail(request, doctor, note);

            try
            {
                await _emailSender.SendEmailAsync(patient.Email, subject, body);
            }
            catch
            {
                // Request still released even if email fails
            }

            request.Status = "Released by doctor";
            request.ReleaseDate = DateTime.Now;
            request.ReleaseNote = note;

            await _context.SaveChangesAsync();

            TempData["Success"] = mode == "InviteConsultation"
                ? "Patient invited for a consultation and notified by email."
                : "Results released to the patient by email.";

            return RedirectToAction(nameof(PatientResults), new { requestId });
        }
        [HttpGet]
        public async Task<IActionResult> ResultsPdf(int requestId)
        {
            // 🆕 Password gate — must have unlocked this session
            if (HttpContext.Session.GetString("DoctorResultsAccess") != "Granted")
            {
                return RedirectToAction(nameof(ResultsAccess), new { requestId });
            }

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(i => i.TestResults)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();
            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            var patient = request.Patient;

            byte[] pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));
                    page.Header().Column(col =>
                    {
                        col.Item().Background("#0d6b63").Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NMB HAEMATOLOGY LABORATORY")
                                    .FontSize(16).Bold().FontColor("#ffffff");
                                c.Item().Text("Test Results Report")
                                    .FontSize(11).FontColor("#d1ede8");
                            });
                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Request #{request.RequestId}")
                                    .FontSize(11).Bold().FontColor("#ffffff");
                                c.Item().Text(request.RequestDate.ToString("dd MMM yyyy"))
                                    .FontSize(10).FontColor("#d1ede8");
                                c.Item().Text($"Urgency: {request.Urgency}")
                                    .FontSize(10).FontColor("#d1ede8");
                            });
                        });
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(8).Text("Patient Information")
                            .FontSize(12).Bold().FontColor("#0d6b63");

                        col.Item().Border(1).BorderColor("#e3e8ee").Padding(12).Column(inner =>
                        {
                            inner.Item().Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Name: {patient.Name} {patient.Surname}").FontSize(10);
                                    c.Item().Text($"ID Number: {patient.IDNumber}").FontSize(10);
                                    c.Item().Text($"Date of Birth: {patient.DOB:dd MMM yyyy}").FontSize(10);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Email: {patient.Email}").FontSize(10);
                                    c.Item().Text($"Cellphone: {patient.CellphoneNumber}").FontSize(10);
                                    c.Item().Text($"Requested by: Dr. {doctor.FirstName} {doctor.LastName}").FontSize(10);
                                });
                            });
                        });

                        col.Item().PaddingTop(20).PaddingBottom(8).Text("Test Results")
                            .FontSize(12).Bold().FontColor("#0d6b63");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#f3faf9").Padding(8)
                                    .Text("Test").FontSize(9).Bold().FontColor("#0d6b63");
                                header.Cell().Background("#f3faf9").Padding(8)
                                    .Text("Result").FontSize(9).Bold().FontColor("#0d6b63");
                                header.Cell().Background("#f3faf9").Padding(8)
                                    .Text("Units").FontSize(9).Bold().FontColor("#0d6b63");
                                header.Cell().Background("#f3faf9").Padding(8)
                                    .Text("Reference").FontSize(9).Bold().FontColor("#0d6b63");
                                header.Cell().Background("#f3faf9").Padding(8)
                                    .Text("Flag").FontSize(9).Bold().FontColor("#0d6b63");
                            });

                            foreach (var item in request.TestRequestItems.OrderBy(i => i.TestType?.Name))
                            {
                                var result = item.TestResults?
                                    .OrderByDescending(r => r.DateCaptured)
                                    .FirstOrDefault();

                                bool abnormal = result?.IsAbnormal ?? false;
                                string flagText =
                                    result == null ? "Pending" :
                                    result.IsAbnormal ? "ABNORMAL" : "Normal";

                                string bg = abnormal ? "#fff6f6" : "#ffffff";
                                string flagColor = abnormal ? "#b42318" : (result == null ? "#92400e" : "#0d6b63");

                                table.Cell().Background(bg).Padding(8)
                                    .Text(item.TestType?.Name ?? "Test").FontSize(10);
                                table.Cell().Background(bg).Padding(8)
                                    .Text(result?.ResultValue ?? "—").FontSize(10).Bold();
                                table.Cell().Background(bg).Padding(8)
                                    .Text(result?.Units ?? "—").FontSize(10);
                                table.Cell().Background(bg).Padding(8)
                                    .Text(result?.ReferenceRange ?? "—").FontSize(10);
                                table.Cell().Background(bg).Padding(8)
                                    .Text(flagText).FontSize(9).Bold().FontColor(flagColor);
                            }
                        });

                        col.Item().PaddingTop(24).Text(
                            "This report was generated by NMB Haematology Laboratory. " +
                            "For clinical questions, please contact your referring doctor.")
                            .FontSize(8).FontColor("#7d8e8a");
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ").FontSize(8).FontColor("#7d8e8a");
                        text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                            .FontSize(8).FontColor("#7d8e8a");
                    });
                });
            }).GeneratePdf();

            string fileName =
                $"Results_Request{request.RequestId}_{patient.Surname}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        // =============================================================
        // RESULTS ACCESS — Password gate before viewing results
        // =============================================================
        private const string ResultsAccessSessionKey = "DoctorResultsAccess";
        private const string ResultsRequiredPassword = "Password!123";

        [HttpGet]
        public IActionResult ResultsAccess(int requestId)
        {
            // Already authenticated this session? Go straight through.
            if (HttpContext.Session.GetString(ResultsAccessSessionKey) == "Granted")
            {
                return RedirectToAction(nameof(PatientResults), new { requestId });
            }

            ViewBag.RequestId = requestId;
            return View("ResultsPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResultsAccess(int requestId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Please enter the access password.");
                ViewBag.RequestId = requestId;
                return View("ResultsPassword");
            }

            if (password != ResultsRequiredPassword)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password. Please try again.");
                ViewBag.RequestId = requestId;
                return View("ResultsPassword");
            }

            HttpContext.Session.SetString(ResultsAccessSessionKey, "Granted");

            return RedirectToAction(nameof(PatientResults), new { requestId });
        }

        // Optional — clear the flag on logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LockResults()
        {
            HttpContext.Session.Remove(ResultsAccessSessionKey);
            TempData["Success"] = "Results access has been locked.";
            return RedirectToAction(nameof(Track));
        }
    }
}