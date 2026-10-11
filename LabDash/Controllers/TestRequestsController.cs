using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.ViewModels;

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
        // Loads patient info, allergies, conditions, medications,
        // previous results, and alphabetically-ordered test types.
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

            // -------- Basic patient info --------
            ViewBag.PatientId = patient.PatientID;
            ViewBag.PatientName = $"{patient.Name} {patient.Surname}";
            ViewBag.PatientIDNumber = patient.IDNumber;
            ViewBag.PatientDOB = patient.DOB.ToString("dd MMM yyyy");
            ViewBag.PatientEmail = patient.Email ?? "";
            ViewBag.PatientCell = patient.CellphoneNumber ?? "";
            ViewBag.RequestDate = DateTime.Today;

            // -------- Patient allergies --------
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

            ViewBag.PatientAllergies = allergies;

            // -------- Patient medical conditions --------
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

            ViewBag.PatientConditions = conditions;

            // -------- Patient medications --------
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

            ViewBag.PatientMedications = medications;

            // -------- Previous results (top 20) --------
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

            // -------- Test types, alphabetically by category then name --------
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
        // Request date is locked server-side to DateTime.Today.
        // One barcode per selected test, all unique.
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

            // 2. Validate barcodes — one per test, all unique
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

            // 6. Create the test request — date is server-side today
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

            // 7. Create items — one per selected test
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

            // 9. Confirm items were created
            var itemCount = await _context.TestRequestItems
                .CountAsync(x => x.RequestId == testRequest.RequestId);

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
                $"with {itemCount} laboratory test(s).";

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
        // Request date stays fixed to the original — never changed.
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

            // Note: RequestDate intentionally NOT reassigned — the
            // original date is preserved no matter what the client sends.
            request.Urgency = urgency;
            request.ClinicalNotes = clinicalNotes;
            request.SampleBarcodes = string.Join(",", cleanBarcodes);

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
        // TRACK — all requests by this doctor, grouped by patient
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

            // Summary counts for the top of the page
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
        // RELEASE REQUEST — Doctor marks the results available to patient
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

            // Only allow release once every test item is Completed.
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
        // VIEW REQUEST — single request, live status only
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
        // PATIENT RESULTS — full detail page, only after Completed
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> PatientResults(int requestId)
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
                        .ThenInclude(r => r.CapturedByTechnician)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            // Only allow results viewing when the request is Completed or Released.
            bool canView =
                request.Status == "Completed" ||
                request.Status == "Released by doctor";

            if (!canView)
            {
                TempData["Error"] = "Results are not yet available for this request.";
                return RedirectToAction(nameof(ViewRequest), new { id = requestId });
            }

            return View(request);
        }


        // =============================================================
        // RELEASE WITH NOTE — Sends email to patient (results or invite)
        // =============================================================
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
                return RedirectToAction(nameof(ViewRequest), new { id = requestId });
            }

            var patient = request.Patient;
            if (patient == null || string.IsNullOrWhiteSpace(patient.Email))
            {
                TempData["Error"] = "Patient email is missing. Cannot notify.";
                return RedirectToAction(nameof(ViewRequest), new { id = requestId });
            }

            // Build results summary table for the email
            var sb = new System.Text.StringBuilder();
            sb.Append("<table style=\"width:100%;border-collapse:collapse;margin-top:12px;\">");
            sb.Append("<thead><tr>");
            sb.Append("<th style=\"text-align:left;padding:6px;border-bottom:1px solid #ddd;\">Test</th>");
            sb.Append("<th style=\"text-align:left;padding:6px;border-bottom:1px solid #ddd;\">Result</th>");
            sb.Append("<th style=\"text-align:left;padding:6px;border-bottom:1px solid #ddd;\">Reference</th>");
            sb.Append("<th style=\"text-align:left;padding:6px;border-bottom:1px solid #ddd;\">Flag</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var item in request.TestRequestItems)
            {
                var result = item.TestResults?
                    .OrderByDescending(r => r.DateCaptured)
                    .FirstOrDefault();

                string testName = item.TestType?.Name ?? "Test";
                string value = result?.ResultValue ?? "-";
                string unit = result?.Units ?? "";
                string range = result?.ReferenceRange ?? "-";
                string flag = (result?.IsAbnormal ?? false) ? "ABNORMAL" : "Normal";

                sb.Append("<tr>");
                sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{System.Net.WebUtility.HtmlEncode(testName)}</td>");
                sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{System.Net.WebUtility.HtmlEncode(value)} {System.Net.WebUtility.HtmlEncode(unit)}</td>");
                sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{System.Net.WebUtility.HtmlEncode(range)}</td>");
                sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{flag}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            string safeName = System.Net.WebUtility.HtmlEncode(patient.Name);
            string safeDoctor = System.Net.WebUtility.HtmlEncode(
                $"Dr. {doctor.FirstName} {doctor.LastName}");
            string safeNote = string.IsNullOrWhiteSpace(note)
                ? ""
                : $"<p style=\"background:#f4faf9;border-left:4px solid #0d6b63;padding:10px 14px;border-radius:8px;\">{System.Net.WebUtility.HtmlEncode(note)}</p>";

            string subject;
            string body;

            if (mode == "InviteConsultation")
            {
                subject = "Please book an appointment to discuss your results";
                body = $@"
            <p>Dear {safeName},</p>
            <p>Your recent laboratory results have been reviewed by {safeDoctor}.</p>
            <p>We would like you to book an appointment to discuss the results in person.</p>
            {safeNote}
            <p>Please contact the practice to arrange a suitable time.</p>
            <p>Regards,<br/>{safeDoctor}<br/>NMB Haematology Laboratory</p>";
            }
            else
            {
                subject = "Your Laboratory Results";
                body = $@"
            <p>Dear {safeName},</p>
            <p>Your recent laboratory results have been reviewed by {safeDoctor}.</p>
            {safeNote}
            <p><strong>Your results:</strong></p>
            {sb}
            <p style=""margin-top:16px;font-size:12px;color:#7d8e8a;"">
                Please contact your doctor if you have any questions about these results.
            </p>
            <p>Regards,<br/>{safeDoctor}<br/>NMB Haematology Laboratory</p>";
            }

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

            return RedirectToAction(nameof(PatientResults), new { requestId = requestId });
        }
    }
}