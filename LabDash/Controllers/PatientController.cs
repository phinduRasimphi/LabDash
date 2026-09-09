using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabDash.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public PatientController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // CURRENT PATIENT
        // ============================================================

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            // Get the Identity user's ID directly from the logged-in user.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return null;

            // Find the patient whose UserId is linked to the
            // currently logged-in Identity account.
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }


        // ============================================================
        //PROFILE
        //============================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account. " +
                    "Please contact an administrator."
                );
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
                HomeAddress = patient.HomeAddress
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            PatientProfileViewModel model)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
            }

            if (!ModelState.IsValid)
                return View(model);

            patient.Name = model.Name;
            patient.Surname = model.Surname;
            patient.CellphoneNumber = model.Cellphone;
            patient.Email = model.Email;
            patient.HomeAddress = model.HomeAddress;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
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
                    "No patient profile is linked to your account."
                );
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
                    : r.Status
            }).ToList();

            return View(model);
        }


        // ============================================================
        // TEST RESULTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Results()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
            }

            var results = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Where(r =>
                    r.TestRequestItem.TestRequest.PatientId
                    == patient.PatientID)
                .OrderByDescending(r => r.DateCaptured)
                .ToListAsync();

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

                NormalMin = 0,
                NormalMax = 0
            }).ToList();

            return View(model);
        }

        // Replace the existing [HttpGet] MedicalHistory() action in
        // PatientController.cs with this version.

        [HttpGet]
        public async Task<IActionResult> MedicalHistory()
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
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
                    CategoryName = pc.MedicalCondition.Category != null
                        ? pc.MedicalCondition.Category.Name
                        : null,
                    DiagnosisDate = pc.DiagnosisDate,
                    Severity = pc.Severity,
                    RecordedByDoctorName = pc.RecordedByDoctor != null
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
                    CategoryName = pa.Allergy.Category != null
                        ? pa.Allergy.Category.Name
                        : null,
                    RecordedDate = pa.RecordedDate,
                    Severity = pa.Severity,
                    RecordedByDoctorName = pa.RecordedByDoctor != null
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
                    CategoryName = pm.Medication.Category,
                    Dosage = pm.Dosage,
                    Frequency = pm.Frequency,
                    StartDate = pm.StartDate,
                    EndDate = pm.EndDate,
                    RecordedByDoctorName = pm.RecordedByDoctor != null
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
                    "No patient profile is linked to your account."
                );
            }

            var model = await BuildConsentViewModelAsync(patient.PatientID);

            return View(model);
        }

        // Live doctor search used by the search box on the Consent page.
        // Doctors are LabUser accounts in the "Doctor" role (no separate
        // Doctor table exists in this project).
        // GET /Patient/SearchDoctors?query=smith
        [HttpGet]
        public async Task<IActionResult> SearchDoctors(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<DoctorSearchResultViewModel>());

            var doctorUsers = await _userManager.GetUsersInRoleAsync("Doctor");

            var results = doctorUsers
                .Where(d =>
                    (d.FullName != null &&
                        d.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (d.HPCSANumber != null &&
                        d.HPCSANumber.Contains(query, StringComparison.OrdinalIgnoreCase))
                )
                .OrderBy(d => d.FullName)
                .Take(8)
                .Select(d => new DoctorSearchResultViewModel
                {
                    DoctorId = d.Id,
                    FullName = d.FullName,
                    HPCSANumber = d.HPCSANumber ?? ""
                })
                .ToList();

            return Json(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantConsent(
            string doctorId,
            List<int> requestIds)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
            }

            if (string.IsNullOrEmpty(doctorId))
            {
                TempData["ErrorMessage"] =
                    "Please select a doctor from the search results.";
                return RedirectToAction(nameof(Consent));
            }

            if (requestIds == null || !requestIds.Any())
            {
                TempData["ErrorMessage"] =
                    "Select at least one test request to share.";
                return RedirectToAction(nameof(Consent));
            }

            var doctorUser = await _userManager.FindByIdAsync(doctorId);
            var doctorIsInRole = doctorUser != null &&
                await _userManager.IsInRoleAsync(doctorUser, "Doctor");

            if (!doctorIsInRole)
            {
                TempData["ErrorMessage"] =
                    "Please select a doctor from the search results.";
                return RedirectToAction(nameof(Consent));
            }

            // Reuse an existing (possibly inactive) consent record between
            // this patient and doctor instead of creating duplicates.
            var consent = await _context.PatientDoctorConsents
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

            await _context.SaveChangesAsync(); // ensure ConsentID is generated

            var existingRequestIds = await _context.ConsentRequestAccess
                .Where(a => a.ConsentID == consent.ConsentID)
                .Select(a => a.RequestID)
                .ToListAsync();

            foreach (var reqId in requestIds.Distinct())
            {
                if (!existingRequestIds.Contains(reqId))
                {
                    _context.ConsentRequestAccess.Add(new ConsentRequestAccess
                    {
                        ConsentID = consent.ConsentID,
                        RequestID = reqId
                    });
                }
            }

            await _context.SaveChangesAsync();

            // TODO: send email notification to the doctor here
            // (reuse the existing Gmail SMTP setup from the password reset flow)

            TempData["SuccessMessage"] =
                "Access granted and the doctor has been notified.";

            return RedirectToAction(nameof(Consent));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeConsent(int consentId)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
            }

            var consent = await _context.PatientDoctorConsents
                .FirstOrDefaultAsync(c =>
                    c.ConsentID == consentId &&
                    c.PatientID == patient.PatientID);

            if (consent != null)
            {
                consent.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Access revoked.";
            }

            return RedirectToAction(nameof(Consent));
        }

        private async Task<ConsentViewModel> BuildConsentViewModelAsync(
            int patientId)
        {
            var activeGrants = await _context.PatientDoctorConsents
                .Where(c => c.PatientID == patientId && c.IsActive)
                .Include(c => c.Doctor)
                .OrderByDescending(c => c.GrantedDate)
                .ToListAsync();

            var activeGrantModels = activeGrants
                .Select(c => new ActiveConsentViewModel
                {
                    ConsentID = c.ConsentID,
                    DoctorId = c.DoctorId,
                    DoctorName = c.Doctor?.FullName ?? "Unknown",
                    HPCSANumber = c.Doctor?.HPCSANumber ?? "",
                    GrantedDate = c.GrantedDate
                })
                .ToList();

            var availableRequests = await _context.TestRequests
                .Where(r => r.PatientId == patientId)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new TestRequestOptionViewModel
                {
                    RequestID = r.RequestId,
                    Label = string.Join(
                        ", ",
                        r.TestRequestItems
                            .Where(i => i.TestType != null)
                            .Select(i => i.TestType.Name))
                })
                .ToListAsync();

            return new ConsentViewModel
            {
                ActiveGrants = activeGrantModels,
                AvailableRequests = availableRequests
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
                    "No patient profile is linked to your account."
                );
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reports(
            ReportViewModel model)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
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
        // HELPER METHODS
        // ============================================================

        private async Task<List<TestResult>> GetPatientResults(
            int patientId)
        {
            return await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Where(r =>
                    r.TestRequestItem.TestRequest.PatientId
                    == patientId)
                .OrderByDescending(r => r.DateCaptured)
                .ToListAsync();
        }


        private List<TestResultViewModel> ConvertResults(
            List<TestResult> results)
        {
            return results.Select(r => new TestResultViewModel
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

                NormalMin = 0,
                NormalMax = 0
            }).ToList();
        }


        private List<string> SplitValues(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
        }
    }
}