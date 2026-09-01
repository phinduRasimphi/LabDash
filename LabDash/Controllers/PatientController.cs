
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

        //[HttpGet]
        //public async Task<IActionResult> Profile()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    var userEmail = User.FindFirstValue(ClaimTypes.Email);

        //    var patient = await _context.Patients
        //        .FirstOrDefaultAsync(p => p.UserId == userId);

        //    if (patient == null)
        //    {
        //        return Content(
        //            $"DEBUG INFORMATION\n\n" +
        //            $"Logged-in User ID: {userId}\n" +
        //            $"Logged-in Email: {userEmail}\n\n" +
        //            $"No Patient found with UserId: {userId}"
        //        );
        //    }

        //    return Content(
        //        $"DEBUG INFORMATION\n\n" +
        //        $"Logged-in User ID: {userId}\n" +
        //        $"Logged-in Email: {userEmail}\n\n" +
        //        $"Patient Found!\n" +
        //        $"PatientID: {patient.PatientID}\n" +
        //        $"Patient Name: {patient.Name} {patient.Surname}\n" +
        //        $"Patient UserId: {patient.UserId}\n" +
        //        $"Patient Email: {patient.Email}"
        //    );
        //}

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

            var model = new MedicalHistoryViewModel
            {
                Conditions = SplitValues(patient.MedicalConditions),

                Allergies = SplitValues(patient.Allergies),

                Medication = SplitValues(patient.Medication)
            };

            return View(model);
        }


        // ============================================================
        // CONSENT
        // ============================================================

        [HttpGet]
        public IActionResult Consent()
        {
            return View(new ConsentViewModel());
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

           