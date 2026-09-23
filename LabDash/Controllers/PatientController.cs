using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notifications;

        public PatientController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender,
            NotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _notifications = notifications;
        }

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }


        // ============================================================
        // PROFILE
        // ============================================================

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
                        .ThenInclude(q => q.RequestingDoctor)

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

                ResultValue =
                    r.ResultValue,

                Unit =
                    r.Units ?? "",

                IsAbnormal =
                    r.IsAbnormal,

                ResultDate =
                    r.DateCaptured,

                Category =
                    r.TestRequestItem.TestType != null
                        ? r.TestRequestItem.TestType.Category ?? ""
                        : "",

                DoctorName =
                    r.TestRequestItem.TestRequest.RequestingDoctor?.FullName
                    ?? "Unknown Doctor",

                // Use the actual TestType reference range
                NormalMin = (double)(r.TestRequestItem.TestType?.ReferenceRangeLow ?? 0m),
                NormalMax = (double)(r.TestRequestItem.TestType?.ReferenceRangeHigh ?? 0m),

                // TestResults contains VerificationNote and Comments.
                // Use VerificationNote first, then Comments.
                TechnicianNotes =
                    !string.IsNullOrWhiteSpace(r.VerificationNote)
                        ? r.VerificationNote
                        : r.Comments ?? ""
            }).ToList();

            return View(model);
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
                    "No patient profile is linked to your account."
                );
            }

            // --------------------------------------------------------
            // CONDITIONS
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // ALLERGIES
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // MEDICATIONS
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // BUILD MEDICAL HISTORY MODEL
            // --------------------------------------------------------

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

            var model =
                await BuildConsentViewModelAsync(patient.PatientID);

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
                    "No patient profile is linked to your account."
                );
            }

            if (string.IsNullOrEmpty(doctorId))
            {
                TempData["ErrorMessage"] =
                    "Please select a doctor.";

                return RedirectToAction(nameof(Consent));
            }

            if (itemIds == null || !itemIds.Any())
            {
                TempData["ErrorMessage"] =
                    "Select at least one test to share.";

                return RedirectToAction(nameof(Consent));
            }

            var doctorUser =
                await _userManager.FindByIdAsync(doctorId);

            var doctorIsInRole =
                doctorUser != null &&
                await _userManager.IsInRoleAsync(
                    doctorUser,
                    "Doctor");

            if (!doctorIsInRole)
            {
                TempData["ErrorMessage"] =
                    "Please select a valid doctor.";

                return RedirectToAction(nameof(Consent));
            }


            // --------------------------------------------------------
            // ONLY ALLOW ITEMS THAT BELONG TO THIS PATIENT'S OWN
            // REQUESTS — never trust the posted ids blindly.
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // FIND EXISTING CONSENT
            // --------------------------------------------------------

            var consent =
                await _context.PatientDoctorConsents
                    .FirstOrDefaultAsync(c =>
                        c.PatientID == patient.PatientID &&
                        c.DoctorId == doctorId);


            // --------------------------------------------------------
            // CREATE NEW CONSENT
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // EXISTING ITEM ACCESS
            // --------------------------------------------------------

            var existingItemIds =
                await _context.ConsentItemAccesses
                    .Where(a =>
                        a.ConsentID == consent.ConsentID)
                    .Select(a => a.TestRequestItemID)
                    .ToListAsync();


            // --------------------------------------------------------
            // ADD ITEM ACCESS
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // IN-APP NOTIFICATION FOR THE DOCTOR
            // --------------------------------------------------------
            await _notifications.SendToUserAsync(
                recipientUserId: doctorId,
                title: "New consent granted",
                message: $"{patient.Name} {patient.Surname} granted you access to " +
                         $"{validItemIds.Count} test item(s).",
                type: "ConsentGranted",
                linkUrl: null,
                relatedConsentId: consent.ConsentID,
                relatedPatientId: patient.PatientID,
                actorUserId: patient.UserId
            );


            // --------------------------------------------------------
            // NOTIFY DOCTOR BY EMAIL
            // --------------------------------------------------------

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
                catch
                {
                    // Consent is still valid even if the notification email
                    // fails to send — don't block the patient's action on
                    // an SMTP hiccup. (Consider logging this via AuditLog.)
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
        public async Task<IActionResult> RevokeConsent(
            int consentId)
        {
            var patient = await GetCurrentPatientAsync();

            if (patient == null)
            {
                return NotFound(
                    "No patient profile is linked to your account."
                );
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

                // Remove every item-access row so the doctor loses access
                // to everything immediately, not just future items.
                _context.ConsentItemAccesses.RemoveRange(
                    consent.ConsentItemAccesses);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Access revoked.";
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
                    "No patient profile is linked to your account."
                );
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

        private async Task<ConsentViewModel>
            BuildConsentViewModelAsync(int patientId)
        {
            // --------------------------------------------------------
            // ACTIVE GRANTS (with the specific items each doctor sees)
            // --------------------------------------------------------

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
                    // Hide grants that were revoked down to zero items —
                    // nothing left for the doctor to see.
                    .Where(c => c.ConsentItemAccesses.Any())
                    .Select(c => new ActiveConsentViewModel
                    {
                        ConsentID = c.ConsentID,

                        DoctorId = c.DoctorId,

                        DoctorName =
                            c.Doctor?.FullName ?? "Unknown",

                        HPCSANumber =
                            c.Doctor?.HPCSANumber ?? "",

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


            // --------------------------------------------------------
            // DOCTORS LINKED TO THIS PATIENT'S OWN REQUESTS
            // (the only doctors that appear in the grant dropdown)
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // AVAILABLE TEST REQUESTS, EXPANDED TO THEIR ITEMS
            // --------------------------------------------------------

            var requests =
                await _context.TestRequests
                    .Where(r =>
                        r.PatientId == patientId)
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
                    "No patient profile is linked to your account."
                );
            }

            var results =
                await GetPatientResults(patient.PatientID);

            var model = new ReportViewModel
            {
                FromDate =
                    DateTime.Today.AddMonths(-1),

                ToDate =
                    DateTime.Today,

                FilteredResults =
                    ConvertResults(results)
            };

            return View(model);
        }


        // ============================================================
        // REPORTS - FILTER
        // ============================================================

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

            var results =
                await GetPatientResults(patient.PatientID);


            results = results
                .Where(r =>
                    r.DateCaptured.Date >= model.FromDate.Date &&
                    r.DateCaptured.Date <= model.ToDate.Date)
                .ToList();


            model.FilteredResults =
                ConvertResults(results);

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
        // HELPER - GET PATIENT RESULTS
        // ============================================================

        private async Task<List<TestResult>>
            GetPatientResults(int patientId)
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


        // ============================================================
        // HELPER - CONVERT RESULTS
        // ============================================================

        private List<TestResultViewModel>
            ConvertResults(List<TestResult> results)
        {
            return results
                .Select(r => new TestResultViewModel
                {
                    RequestID =
                        r.TestRequestItem
                            .TestRequest
                            .RequestId
                            .ToString(),

                    TestName =
                        r.TestRequestItem.TestType != null
                            ? r.TestRequestItem.TestType.Name
                            : "Unknown Test",

                    ResultValue =
                        r.ResultValue,

                    Unit =
                        r.Units ?? "",

                    IsAbnormal =
                        r.IsAbnormal,

                    ResultDate =
                        r.DateCaptured,

                    Category =
    r.TestRequestItem.TestType != null
        ? r.TestRequestItem.TestType.Category ?? ""
        : "",

                    NormalMin = 0,
                    NormalMax = 0
                })
                .ToList();
        }


        // ============================================================
        // HELPER - SPLIT VALUES
        // ============================================================

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