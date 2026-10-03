using LabDash.Areas.Identity.Data;

using LabDash.Models;
using LabDash.Services;
using LabDash.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using System.Security.Claims;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly SignInManager<LabUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notifications;
        private readonly ILogger<PatientController> _logger;

        public PatientController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            SignInManager<LabUser> signInManager,
            IEmailSender emailSender,
            NotificationService notifications,
            ILogger<PatientController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _notifications = notifications;
            _logger = logger;
        }

        // ============================================================
        // GET CURRENT PATIENT
        // ============================================================

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GetCurrentPatientAsync: User ID is null.");
                return null;
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
            {
                _logger.LogWarning(
                    "GetCurrentPatientAsync: No patient found for UserId {UserId}",
                    userId);
            }

            return patient;
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                TempData["ErrorMessage"] =
                    "Your patient profile could not be found.";

                return RedirectToAction("Index", "Dashboard");
            }

            var model = new PatientProfileViewModel
            {
                PatientID = patient.PatientID,
                Name = patient.Name,
                Surname = patient.Surname,
                IDNumber = patient.IDNumber,
                DateOfBirth = patient.DOB,
                Cellphone = patient.CellphoneNumber,
                Email = patient.Email,

                AddressLine1 = patient.AddressLine1 ?? "",
                AddressLine2 = patient.AddressLine2 ?? "",
                Suburb = patient.Suburb ?? "",
                City = patient.City ?? "",
                Province = patient.Province ?? "",
                PostalCode = patient.PostalCode ?? "",

                HomeAddress = patient.HomeAddress ?? ""
            };

            return View(model);
        }

        // ============================================================
        // PROFILE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null)
            {
                TempData["ErrorMessage"] =
                    "Your patient profile could not be found.";

                return RedirectToAction("Index", "Dashboard");
            }

            var model = new PatientProfileViewModel
            {
                PatientID = patient.PatientID,

                Name = patient.Name ?? "",
                Surname = patient.Surname ?? "",
                IDNumber = patient.IDNumber ?? "",
                DateOfBirth = patient.DOB,

                Cellphone = patient.CellphoneNumber ?? "",
                Email = patient.Email ?? user.Email ?? "",

                AddressLine1 = patient.AddressLine1 ?? "",
                AddressLine2 = patient.AddressLine2 ?? "",
                Suburb = patient.Suburb ?? "",
                City = patient.City ?? "",
                Province = patient.Province ?? "",
                PostalCode = patient.PostalCode ?? "",

                HomeAddress = patient.HomeAddress ?? ""
            };

            // --------------------------------------------------------
            // LOAD EXISTING HEALTH INFORMATION
            // --------------------------------------------------------

            model.SelectedMedicalConditionIds =
                await GetSelectedMedicalConditionIds(patient);

            model.SelectedAllergyIds =
                await GetSelectedAllergyIds(patient);

            model.SelectedMedicationIds =
                await GetSelectedMedicationIds(patient);

            // --------------------------------------------------------
            // LOAD "OTHER" VALUES
            // --------------------------------------------------------

            model.OtherMedicalCondition =
                GetOtherValues(
                    patient.MedicalConditions,
                    await _context.MedicalConditions
                        .Where(x => x.IsActive)
                        .Select(x => x.ConditionName)
                        .ToListAsync());

            model.OtherAllergy =
                GetOtherValues(
                    patient.Allergies,
                    await _context.Allergies
                        .Where(x => x.IsActive)
                        .Select(x => x.AllergyName)
                        .ToListAsync());

            model.OtherMedication =
                GetOtherValues(
                    patient.Medication,
                    await _context.Medications
                        .Where(x => x.IsActive)
                        .Select(x => x.MedicationName)
                        .ToListAsync());

            await PopulateProfileOptionsAsync(model);

            return View(model);
        }

        // ============================================================
        // PROFILE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            PatientProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // FIND PATIENT USING IDENTITY USER ID
            // --------------------------------------------------------

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null)
            {
                _logger.LogWarning(
                    "Profile POST: No Patient found for UserId {UserId}",
                    user.Id);

                ModelState.AddModelError(
                    "",
                    "Your patient profile could not be found.");

                await PopulateProfileOptionsAsync(model);

                return View(model);
            }

            _logger.LogInformation(
                "PROFILE UPDATE START - PatientID: {PatientID}, UserId: {UserId}",
                patient.PatientID,
                patient.UserId);

            // --------------------------------------------------------
            // BASIC PATIENT INFORMATION
            // --------------------------------------------------------

            patient.Name = CleanValue(model.Name);
            patient.Surname = CleanValue(model.Surname);

            patient.IDNumber = CleanValue(model.IDNumber);

            patient.DOB = model.DateOfBirth;

            patient.CellphoneNumber = CleanValue(model.Cellphone);

            patient.Email = CleanValue(model.Email);

            // --------------------------------------------------------
            // ADDRESS
            // --------------------------------------------------------

            patient.AddressLine1 = CleanValue(model.AddressLine1);
            patient.AddressLine2 = CleanValue(model.AddressLine2);
            patient.Suburb = CleanValue(model.Suburb);
            patient.City = CleanValue(model.City);
            patient.Province = CleanValue(model.Province);
            patient.PostalCode = CleanValue(model.PostalCode);

            patient.HomeAddress = BuildHomeAddress(model);

            // --------------------------------------------------------
            // HEALTH INFORMATION
            // --------------------------------------------------------

            var selectedConditionNames =
                await _context.MedicalConditions
                    .Where(x =>
                        x.IsActive &&
                        model.SelectedMedicalConditionIds
                            .Contains(x.MedicalConditionId))
                    .OrderBy(x => x.ConditionName)
                    .Select(x => x.ConditionName)
                    .ToListAsync();

            var selectedAllergyNames =
                await _context.Allergies
                    .Where(x =>
                        x.IsActive &&
                        model.SelectedAllergyIds
                            .Contains(x.AllergyId))
                    .OrderBy(x => x.AllergyName)
                    .Select(x => x.AllergyName)
                    .ToListAsync();

            var selectedMedicationNames =
                await _context.Medications
                    .Where(x =>
                        x.IsActive &&
                        model.SelectedMedicationIds
                            .Contains(x.MedicationId))
                    .OrderBy(x => x.MedicationName)
                    .Select(x => x.MedicationName)
                    .ToListAsync();

            // --------------------------------------------------------
            // ADD OTHER VALUES
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.OtherMedicalCondition))
            {
                selectedConditionNames.Add(model.OtherMedicalCondition.Trim());
            }

            if (!string.IsNullOrWhiteSpace(model.OtherAllergy))
            {
                selectedAllergyNames.Add(model.OtherAllergy.Trim());
            }

            if (!string.IsNullOrWhiteSpace(model.OtherMedication))
            {
                selectedMedicationNames.Add(model.OtherMedication.Trim());
            }

            // --------------------------------------------------------
            // SAVE "NONE" IF NOTHING WAS SELECTED
            // --------------------------------------------------------

            patient.MedicalConditions =
                selectedConditionNames.Any()
                    ? string.Join(", ", selectedConditionNames)
                    : "None";

            patient.Allergies =
                selectedAllergyNames.Any()
                    ? string.Join(", ", selectedAllergyNames)
                    : "None";

            patient.Medication =
                selectedMedicationNames.Any()
                    ? string.Join(", ", selectedMedicationNames)
                    : "None";

            // --------------------------------------------------------
            // EXPLICITLY MARK PATIENT AS MODIFIED
            // --------------------------------------------------------

            _context.Entry(patient).State = EntityState.Modified;

            // --------------------------------------------------------
            // SAVE TO DATABASE
            // --------------------------------------------------------

            try
            {
                var affectedRows = await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "SaveChangesAsync affected {AffectedRows} rows.",
                    affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR WHILE SAVING PATIENT PROFILE");

                ModelState.AddModelError(
                    "",
                    "There was an error saving your profile: " + ex.Message);

                await PopulateProfileOptionsAsync(model);

                return View(model);
            }

            // --------------------------------------------------------
            // UPDATE IDENTITY USER
            // --------------------------------------------------------

            user.FirstName = CleanValue(model.Name);
            user.LastName = CleanValue(model.Surname);

            user.PhoneNumb = CleanValue(model.Cellphone);

            user.Email = CleanValue(model.Email);
            user.UserName = CleanValue(model.Email);

            var identityResult = await _userManager.UpdateAsync(user);

            if (!identityResult.Succeeded)
            {
                foreach (var error in identityResult.Errors)
                {
                    _logger.LogWarning(
                        "Identity update error: {Code} - {Description}",
                        error.Code,
                        error.Description);
                }
            }

            // --------------------------------------------------------
            // REFRESH LOGIN
            // --------------------------------------------------------

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // ============================================================
        // POPULATE PROFILE OPTIONS
        // ============================================================

        private async Task PopulateProfileOptionsAsync(
            PatientProfileViewModel model)
        {
            model.MedicalConditionOptions =
                await _context.MedicalConditions
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ConditionName)
                    .ToListAsync();

            model.AllergyOptions =
                await _context.Allergies
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.AllergyName)
                    .ToListAsync();

            model.MedicationOptions =
                await _context.Medications
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.MedicationName)
                    .ToListAsync();
        }

        // ============================================================
        // GET SELECTED MEDICAL CONDITIONS
        // ============================================================

        private async Task<List<int>> GetSelectedMedicalConditionIds(
            Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.MedicalConditions) ||
                patient.MedicalConditions.Equals(
                    "None",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new List<int>();
            }

            var names = SplitValues(patient.MedicalConditions);

            return await _context.MedicalConditions
                .Where(x =>
                    x.IsActive &&
                    names.Contains(x.ConditionName))
                .Select(x => x.MedicalConditionId)
                .ToListAsync();
        }

        // ============================================================
        // GET SELECTED ALLERGIES
        // ============================================================

        private async Task<List<int>> GetSelectedAllergyIds(
            Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.Allergies) ||
                patient.Allergies.Equals(
                    "None",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new List<int>();
            }

            var names = SplitValues(patient.Allergies);

            return await _context.Allergies
                .Where(x =>
                    x.IsActive &&
                    names.Contains(x.AllergyName))
                .Select(x => x.AllergyId)
                .ToListAsync();
        }

        // ============================================================
        // GET SELECTED MEDICATIONS
        // ============================================================

        private async Task<List<int>> GetSelectedMedicationIds(
            Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.Medication) ||
                patient.Medication.Equals(
                    "None",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new List<int>();
            }

            var names = SplitValues(patient.Medication);

            return await _context.Medications
                .Where(x =>
                    x.IsActive &&
                    names.Contains(x.MedicationName))
                .Select(x => x.MedicationId)
                .ToListAsync();
        }

        // ============================================================
        // CLEAN VALUE
        // ============================================================

        private string CleanValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim();
        }

        // ============================================================
        // BUILD HOME ADDRESS
        // ============================================================

        private string BuildHomeAddress(PatientProfileViewModel model)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(model.AddressLine1))
                parts.Add(model.AddressLine1.Trim());

            if (!string.IsNullOrWhiteSpace(model.AddressLine2))
                parts.Add(model.AddressLine2.Trim());

            if (!string.IsNullOrWhiteSpace(model.Suburb))
                parts.Add(model.Suburb.Trim());

            if (!string.IsNullOrWhiteSpace(model.City))
                parts.Add(model.City.Trim());

            if (!string.IsNullOrWhiteSpace(model.Province))
                parts.Add(model.Province.Trim());

            if (!string.IsNullOrWhiteSpace(model.PostalCode))
                parts.Add(model.PostalCode.Trim());

            return string.Join(", ", parts);
        }

        // ============================================================
        // GET OTHER VALUES
        // ============================================================

        private string GetOtherValues(
            string? storedValue,
            List<string> knownValues)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return "";

            if (storedValue.Equals("None", StringComparison.OrdinalIgnoreCase))
                return "";

            var storedValues = SplitValues(storedValue);

            var otherValues =
                storedValues
                    .Where(value =>
                        !knownValues.Any(known =>
                            known.Equals(
                                value,
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            return string.Join(", ", otherValues);
        }

        // ============================================================
        // SPLIT VALUES
        // ============================================================

        private List<string> SplitValues(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        // ============================================================
        // TEST REQUESTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var requests = await _context.TestRequests
                .Where(r => r.PatientId == patient.PatientID)
                .Include(r => r.RequestingDoctor)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var model = requests.Select(r => new TestRequestViewModel
            {
                RequestID = r.RequestId.ToString(),

                RequestDate = r.RequestDate,

                DoctorName = r.RequestingDoctor != null
                    ? r.RequestingDoctor.FullName
                    : "Unknown",

                Tests = r.TestRequestItems
                    .Where(i => i.TestType != null)
                    .Select(i => i.TestType.Name)
                    .ToList(),

                Urgency = string.IsNullOrWhiteSpace(r.Urgency)
                    ? "Routine"
                    : r.Urgency,

                Status = string.IsNullOrWhiteSpace(r.Status)
                    ? "Submitted"
                    : r.Status,

                // Requires these two properties on TestRequestViewModel
                CancelReason = r.CancellationReason,
                ReleaseDate = r.ReleaseDate
            }).ToList();

            return View(model);
        }

        // ============================================================
        // CANCEL REQUEST (patient, only before testing starts)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int requestId, string? reason)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            // Ownership check: only this patient's own request can be found
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.PatientId == patient.PatientID);

            if (request == null)
            {
                TempData["ErrorMessage"] = "That request could not be found.";

                return RedirectToAction(nameof(Requests));
            }

            // Status check: Submitted or Samples Received only
            var status = (request.Status ?? "").Trim().ToLowerInvariant();

            var canCancel =
                string.IsNullOrEmpty(status) ||
                status == "submitted" ||
                status == "samples received" ||
                status == "sample received";

            if (!canCancel)
            {
                TempData["ErrorMessage"] =
                    "This request can no longer be cancelled because testing has already started.";

                return RedirectToAction(nameof(Requests));
            }

            request.Status = "Cancelled";

            request.CancellationReason = string.IsNullOrWhiteSpace(reason)
                ? "Cancelled by patient"
                : "Cancelled by patient: " + reason.Trim();

            request.ReleaseDate = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request #{requestId} was cancelled.";

            return RedirectToAction(nameof(Requests));
        }

        // ============================================================
        // RESULTS
        // Only results the doctor has released are ever returned.
        // Optional requestId = open the results of one request.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Results(int? requestId = null)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var query = _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(q => q.RequestingDoctor)

                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)

                .Where(r =>
                    r.TestRequestItem.TestRequest.PatientId
                    == patient.PatientID &&

                    // RELEASE GATE: only results the doctor has released
                    r.TestRequestItem.TestRequest.Status.Contains("Release"));

            if (requestId.HasValue)
            {
                query = query.Where(r =>
                    r.TestRequestItem.TestRequest.RequestId
                    == requestId.Value);
            }

            var results = await query
                .OrderByDescending(r => r.DateCaptured)
                .ToListAsync();

            if (requestId.HasValue && !results.Any())
            {
                TempData["ErrorMessage"] =
                    "Results for that request are not available yet.";

                return RedirectToAction(nameof(Requests));
            }

            var model = results.Select(r => new TestResultViewModel
            {
                RequestID =
                    r.TestRequestItem.TestRequest.RequestId.ToString(),

                TestName =
                    r.TestRequestItem.TestType != null
                        ? r.TestRequestItem.TestType.Name
                        : "Unknown Test",

                ResultValue = r.ResultValue,

                Unit = r.Units ?? "",

                IsAbnormal = r.IsAbnormal,

                ResultDate = r.DateCaptured,

                Category =
                    r.TestRequestItem.TestType != null
                        ? r.TestRequestItem.TestType.Category ?? ""
                        : "",

                DoctorName =
                    r.TestRequestItem.TestRequest.RequestingDoctor?.FullName
                    ?? "Unknown Doctor",

                NormalMin = (double)(r.TestRequestItem.TestType?.ReferenceRangeLow ?? 0m),
                NormalMax = (double)(r.TestRequestItem.TestType?.ReferenceRangeHigh ?? 0m),

                TechnicianNotes =
                    !string.IsNullOrWhiteSpace(r.VerificationNote)
                        ? r.VerificationNote
                        : r.Comments ?? ""
            }).ToList();

            // Lets the view switch to "single request" mode
            ViewBag.RequestFilter = requestId;

            return View(model);
        }

        // ============================================================
        // DOWNLOAD RESULTS PDF (one released request)
        // Needs NuGet package QuestPDF and, once in Program.cs:
        //   QuestPDF.Settings.License = LicenseType.Community;
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadResultsPdf(int requestId)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var results = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(q => q.RequestingDoctor)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Where(r =>
                    r.TestRequestItem.TestRequest.PatientId == patient.PatientID &&
                    r.TestRequestItem.TestRequest.RequestId == requestId &&
                    // same release gate as Results()
                    r.TestRequestItem.TestRequest.Status.Contains("Release"))
                .OrderBy(r => r.TestRequestItem.TestType.Category)
                .ThenBy(r => r.TestRequestItem.TestType.Name)
                .ToListAsync();

            if (!results.Any())
            {
                TempData["ErrorMessage"] =
                    "Results for that request are not available yet.";

                return RedirectToAction(nameof(Requests));
            }

            var req = results[0].TestRequestItem.TestRequest;

            static IContainer Cell(IContainer c) =>
                c.BorderBottom(1)
                 .BorderColor(Colors.Grey.Lighten2)
                 .PaddingVertical(5)
                 .PaddingHorizontal(4);

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Laboratory Test Report")
                            .FontSize(20).Bold().FontColor("#0A5850");

                        col.Item().Text(
                            $"{patient.Name} {patient.Surname}  |  ID {patient.IDNumber}");

                        col.Item().Text(
                            $"Request #{req.RequestId}  |  Requested {req.RequestDate:dd MMM yyyy}" +
                            (req.ReleaseDate.HasValue
                                ? $"  |  Released {req.ReleaseDate:dd MMM yyyy}"
                                : "") +
                            $"  |  Dr. {req.RequestingDoctor?.FullName ?? "Unknown"}");
                    });

                    page.Content().PaddingVertical(14).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(Cell).Text("Test").Bold();
                                h.Cell().Element(Cell).Text("Result").Bold();
                                h.Cell().Element(Cell).Text("Reference range").Bold();
                                h.Cell().Element(Cell).Text("Status").Bold();
                            });

                            foreach (var r in results)
                            {
                                var type = r.TestRequestItem.TestType;

                                var range =
                                    type != null &&
                                    (type.ReferenceRangeLow ?? 0m) + (type.ReferenceRangeHigh ?? 0m) > 0m
                                        ? $"{type.ReferenceRangeLow:0.##} - {type.ReferenceRangeHigh:0.##}"
                                        : "-";

                                table.Cell().Element(Cell)
                                    .Text(type?.Name ?? "Unknown Test");

                                table.Cell().Element(Cell)
                                    .Text($"{r.ResultValue} {r.Units}");

                                table.Cell().Element(Cell).Text(range);

                                table.Cell().Element(Cell)
                                    .Text(r.IsAbnormal ? "Abnormal" : "Normal")
                                    .FontColor(r.IsAbnormal ? "#B0463F" : "#2E8B57")
                                    .Bold();
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(req.ReleaseNote))
                        {
                            col.Item().PaddingTop(16).Text("Doctor's note").Bold();
                            col.Item().Text(req.ReleaseNote);
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(x =>
                            x.FontSize(8).FontColor(Colors.Grey.Darken1));

                        t.Span("NMB Haematology Laboratory  |  Page ");
                        t.CurrentPageNumber();
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"results-request-{requestId}.pdf");
        }

        // ============================================================
        // MEDICAL HISTORY
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MedicalHistory()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var conditions = await _context.PatientMedicalConditions
                .Where(pc => pc.PatientID == patient.PatientID)
                .Include(pc => pc.MedicalCondition)
                    .ThenInclude(mc => mc!.Category)
                .Include(pc => pc.RecordedByDoctor)
                .Where(pc => pc.MedicalCondition != null)
                .OrderByDescending(pc => pc.DiagnosisDate)
                .Select(pc => new ConditionRecordViewModel
                {
                    Name = pc.MedicalCondition!.ConditionName,

                    CategoryName =
                        pc.MedicalCondition.Category != null
                            ? pc.MedicalCondition.Category.Name
                            : null,

                    DiagnosisDate = pc.DiagnosisDate,

                    Severity = pc.Severity,

                    RecordedByDoctorName =
                        pc.RecordedByDoctor != null
                            ? pc.RecordedByDoctor.FullName
                            : null,

                    Notes = pc.Notes
                })
                .ToListAsync();

            var allergies = await _context.PatientAllergies
                .Where(pa => pa.PatientID == patient.PatientID)
                .Include(pa => pa.Allergy)
                    .ThenInclude(a => a!.Category)
                .Include(pa => pa.RecordedByDoctor)
                .Where(pa => pa.Allergy != null)
                .OrderByDescending(pa => pa.RecordedDate)
                .Select(pa => new AllergyRecordViewModel
                {
                    Name = pa.Allergy!.AllergyName,

                    CategoryName =
                        pa.Allergy.Category != null
                            ? pa.Allergy.Category.Name
                            : null,

                    RecordedDate = pa.RecordedDate,

                    Severity = pa.Severity,

                    RecordedByDoctorName =
                        pa.RecordedByDoctor != null
                            ? pa.RecordedByDoctor.FullName
                            : null,

                    Notes = pa.Notes
                })
                .ToListAsync();

            var medications = await _context.PatientMedications
                .Where(pm => pm.PatientID == patient.PatientID)
                .Include(pm => pm.Medication)
                .Include(pm => pm.RecordedByDoctor)
                .Where(pm => pm.Medication != null)
                .OrderByDescending(pm => pm.StartDate)
                .Select(pm => new MedicationRecordViewModel
                {
                    Name = pm.Medication!.MedicationName,

                    CategoryName =
                        pm.Medication.Category != null
                            ? pm.Medication.Category.Name
                            : null,

                    Dosage = pm.Dosage,

                    Frequency = pm.Frequency,

                    StartDate = pm.StartDate,

                    EndDate = pm.EndDate,

                    RecordedByDoctorName =
                        pm.RecordedByDoctor != null
                            ? pm.RecordedByDoctor.FullName
                            : null,

                    Notes = pm.Notes
                })
                .ToListAsync();

            var model = new MedicalHistoryViewModel
            {
                Conditions = conditions,
                Allergies = allergies,
                Medications = medications
            };

            return View(model);
        }

        // ============================================================
        // CONSENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Consent()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var model = await BuildConsentViewModelAsync(patient.PatientID);

            return View(model);
        }

        // ============================================================
        // GRANT CONSENT (per test item, not whole request)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantConsent(
            string doctorId,
            List<int> itemIds)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            if (string.IsNullOrEmpty(doctorId))
            {
                TempData["ErrorMessage"] = "Please select a doctor.";

                return RedirectToAction(nameof(Consent));
            }

            if (itemIds == null || !itemIds.Any())
            {
                TempData["ErrorMessage"] = "Select at least one test to share.";

                return RedirectToAction(nameof(Consent));
            }

            var doctorUser = await _userManager.FindByIdAsync(doctorId);

            var doctorIsInRole =
                doctorUser != null &&
                await _userManager.IsInRoleAsync(doctorUser, "Doctor");

            if (!doctorIsInRole)
            {
                TempData["ErrorMessage"] = "Please select a valid doctor.";

                return RedirectToAction(nameof(Consent));
            }

            // Only allow items that belong to this patient's own requests —
            // never trust the posted ids blindly.
            var validItemIds = await _context.TestRequestItems
                .Where(i =>
                    i.TestRequest.PatientId == patient.PatientID &&
                    itemIds.Contains(i.TestRequestItemId))
                .Select(i => i.TestRequestItemId)
                .ToListAsync();

            if (!validItemIds.Any())
            {
                TempData["ErrorMessage"] =
                    "None of the selected tests could be found.";

                return RedirectToAction(nameof(Consent));
            }

            var consent =
                await _context.PatientDoctorConsents
                    .FirstOrDefaultAsync(c =>
                        c.PatientID == patient.PatientID &&
                        c.DoctorId == doctorId);

            if (consent == null)
            {
                consent = new PatientDoctorConsent
                {
                    PatientID = patient.PatientID,
                    DoctorId = doctorId,
                    GrantedDate = DateTime.Now,
                    IsActive = true
                };

                _context.PatientDoctorConsents.Add(consent);
            }
            else
            {
                consent.IsActive = true;
                consent.GrantedDate = DateTime.Now;
            }

            // Save first so ConsentID is generated.
            await _context.SaveChangesAsync();

            var existingItemIds =
                await _context.ConsentItemAccesses
                    .Where(a => a.ConsentID == consent.ConsentID)
                    .Select(a => a.TestRequestItemID)
                    .ToListAsync();

            foreach (var itemId in validItemIds.Distinct())
            {
                if (!existingItemIds.Contains(itemId))
                {
                    _context.ConsentItemAccesses.Add(
                        new ConsentItemAccess
                        {
                            ConsentID = consent.ConsentID,
                            TestRequestItemID = itemId
                        });
                }
            }

            await _context.SaveChangesAsync();

            // Notify doctor by email
            if (doctorUser != null && !string.IsNullOrWhiteSpace(doctorUser.Email))
            {
                var subject = "A patient has granted you access to test results";

                var body =
                    $"<p>Dear Dr. {doctorUser.LastName},</p>" +
                    $"<p><strong>{patient.Name} {patient.Surname}</strong> has granted you " +
                    $"access to {validItemIds.Count} test result item(s) via the LabDash patient portal.</p>" +
                    "<p>You can view them by logging into the Doctor Portal.</p>" +
                    "<p>This access can be revoked by the patient at any time.</p>";

                try
                {
                    await _emailSender.SendEmailAsync(
                        doctorUser.Email,
                        subject,
                        body);
                }
                catch (Exception ex)
                {
                    // Consent is still valid even if the notification email fails.
                    _logger.LogWarning(
                        ex,
                        "Consent granted but the notification email failed to send.");
                }
            }

            TempData["SuccessMessage"] =
                "Access granted and the doctor has been notified.";

            return RedirectToAction(nameof(Consent));
        }

        // ============================================================
        // REVOKE CONSENT — whole doctor (all items, instantly)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeConsent(int consentId)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var consent =
                await _context.PatientDoctorConsents
                    .Include(c => c.ConsentItemAccesses)
                    .FirstOrDefaultAsync(c =>
                        c.ConsentID == consentId &&
                        c.PatientID == patient.PatientID);

            if (consent != null)
            {
                consent.IsActive = false;

                _context.ConsentItemAccesses.RemoveRange(
                    consent.ConsentItemAccesses);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Access revoked.";
            }

            return RedirectToAction(nameof(Consent));
        }

        // ============================================================
        // REVOKE ITEM ACCESS — single test, instantly
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeItemAccess(
            int consentItemAccessId)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var access =
                await _context.ConsentItemAccesses
                    .Include(a => a.Consent)
                    .FirstOrDefaultAsync(a =>
                        a.ConsentItemAccessID == consentItemAccessId &&
                        a.Consent != null &&
                        a.Consent.PatientID == patient.PatientID);

            if (access != null)
            {
                _context.ConsentItemAccesses.Remove(access);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Access to that test was revoked.";
            }

            return RedirectToAction(nameof(Consent));
        }

        // ============================================================
        // BUILD CONSENT VIEW MODEL
        // ============================================================

        private async Task<ConsentViewModel> BuildConsentViewModelAsync(
            int patientId)
        {
            var activeGrants =
                await _context.PatientDoctorConsents
                    .Where(c =>
                        c.PatientID == patientId &&
                        c.IsActive)
                    .Include(c => c.Doctor)
                    .Include(c => c.ConsentItemAccesses)
                        .ThenInclude(a => a.TestRequestItem)
                            .ThenInclude(i => i!.TestType)
                    .Include(c => c.ConsentItemAccesses)
                        .ThenInclude(a => a.TestRequestItem)
                            .ThenInclude(i => i!.TestRequest)
                    .OrderByDescending(c => c.GrantedDate)
                    .ToListAsync();

            var activeGrantModels =
                activeGrants
                    // Hide grants that were revoked down to zero items
                    .Where(c => c.ConsentItemAccesses.Any())
                    .Select(c => new ActiveConsentViewModel
                    {
                        ConsentID = c.ConsentID,

                        DoctorId = c.DoctorId,

                        DoctorName = c.Doctor?.FullName ?? "Unknown",

                        HPCSANumber = c.Doctor?.HPCSANumber ?? "",

                        GrantedDate = c.GrantedDate,

                        GrantedItems = c.ConsentItemAccesses
                            .Where(a => a.TestRequestItem != null)
                            .Select(a => new GrantedItemViewModel
                            {
                                ConsentItemAccessID = a.ConsentItemAccessID,

                                RequestID = a.TestRequestItem!.RequestId,

                                TestName =
                                    a.TestRequestItem.TestType != null
                                        ? a.TestRequestItem.TestType.Name
                                        : "Unknown Test"
                            })
                            .OrderBy(i => i.RequestID)
                            .ThenBy(i => i.TestName)
                            .ToList()
                    })
                    .ToList();

            var linkedDoctorIds =
                await _context.TestRequests
                    .Where(r => r.PatientId == patientId)
                    .Select(r => r.RequestingDoctorId)
                    .Distinct()
                    .ToListAsync();

            var linkedDoctors =
                await _context.Users
                    .Where(u => linkedDoctorIds.Contains(u.Id))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new DoctorSearchResultViewModel
                    {
                        DoctorId = u.Id,

                        FullName = u.FirstName + " " + u.LastName,

                        HPCSANumber = u.HPCSANumber ?? ""
                    })
                    .ToListAsync();

            var requests =
                await _context.TestRequests
                    .Where(r => r.PatientId == patientId)
                    .Include(r => r.TestRequestItems)
                        .ThenInclude(i => i.TestType)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

            var availableRequests =
                requests
                    .Where(r => r.TestRequestItems.Any(i => i.TestType != null))
                    .Select(r => new TestRequestOptionViewModel
                    {
                        RequestID = r.RequestId,

                        Label = string.Join(
                            ", ",
                            r.TestRequestItems
                                .Where(i => i.TestType != null)
                                .Select(i => i.TestType.Name)),

                        Items = r.TestRequestItems
                            .Where(i => i.TestType != null)
                            .Select(i => new TestRequestItemOptionViewModel
                            {
                                TestRequestItemID = i.TestRequestItemId,

                                TestName = i.TestType.Name
                            })
                            .ToList()
                    })
                    .ToList();

            return new ConsentViewModel
            {
                ActiveGrants = activeGrantModels,

                AvailableRequests = availableRequests,

                LinkedDoctors = linkedDoctors
            };
        }

        // ============================================================
        // REPORTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            var results = await GetPatientResults(patient.PatientID);

            var model = new ReportViewModel
            {
                FromDate = DateTime.Today.AddMonths(-1),

                ToDate = DateTime.Today,

                FilteredResults = ConvertResults(results)
            };

            return View(model);
        }

        // ============================================================
        // REPORTS - FILTER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reports(ReportViewModel model)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var results = await GetPatientResults(patient.PatientID);

            results = results
                .Where(r =>
                    r.DateCaptured.Date >= model.FromDate.Date &&
                    r.DateCaptured.Date <= model.ToDate.Date)
                .ToList();

            model.FilteredResults = ConvertResults(results);

            return View(model);
        }

        // ============================================================
        // PRIVACY
        // ============================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        // ============================================================
        // HELPER - GET PATIENT RESULTS (released results only)
        // ============================================================

        private async Task<List<TestResult>> GetPatientResults(int patientId)
        {
            return await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)

                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)

                .Where(r =>
                    r.TestRequestItem.TestRequest.PatientId == patientId &&
                    r.TestRequestItem.TestRequest.Status.Contains("Release"))

                .OrderByDescending(r => r.DateCaptured)

                .ToListAsync();
        }

        // ============================================================
        // HELPER - CONVERT RESULTS
        // ============================================================

        private List<TestResultViewModel> ConvertResults(
            List<TestResult> results)
        {
            return results
                .Select(r => new TestResultViewModel
                {
                    RequestID =
                        r.TestRequestItem.TestRequest.RequestId.ToString(),

                    TestName =
                        r.TestRequestItem.TestType != null
                            ? r.TestRequestItem.TestType.Name
                            : "Unknown Test",

                    ResultValue = r.ResultValue,

                    Unit = r.Units ?? "",

                    IsAbnormal = r.IsAbnormal,

                    ResultDate = r.DateCaptured,

                    Category =
                        r.TestRequestItem.TestType != null
                            ? r.TestRequestItem.TestType.Category ?? ""
                            : "",

                    NormalMin = (double)(r.TestRequestItem.TestType?.ReferenceRangeLow ?? 0m),
                    NormalMax = (double)(r.TestRequestItem.TestType?.ReferenceRangeHigh ?? 0m)
                })
                .ToList();
        }
    }
}
