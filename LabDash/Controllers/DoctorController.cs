using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly SignInManager<LabUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IAuditService _audit;
        private readonly IRazorEmailRenderer _emailRenderer;

        public DoctorController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            SignInManager<LabUser> signInManager,
            IEmailSender emailSender,
            IAuditService audit,
            IRazorEmailRenderer emailRenderer)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _audit = audit;
            _emailRenderer = emailRenderer;
        }

        // =========================================================
        // MANAGE PATIENTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ManagePatients(string? searchIDNumber)
        {
            var vm = new ManagePatientsViewModel
            {
                SearchIDNumber = searchIDNumber
            };

            if (!string.IsNullOrWhiteSpace(searchIDNumber))
            {
                vm.HasSearched = true;

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IDNumber == searchIDNumber.Trim());

                if (patient != null)
                {
                    vm.SearchResult = new PatientDetailsViewModel
                    {
                        PatientID = patient.PatientID,
                        UserId = patient.UserId,
                        Name = patient.Name,
                        Surname = patient.Surname,
                        IDNumber = patient.IDNumber,
                        CellphoneNumber = patient.CellphoneNumber,
                        DOB = patient.DOB,
                        Email = patient.Email,
                        HomeAddress = patient.AddressLine1,
                        MedicalConditions = patient.MedicalConditions,
                        Allergies = patient.Allergies,
                        Medication = patient.Medication
                    };
                }
            }

            return View(vm);
        }

        // =========================================================
        // CONSENTED PATIENTS (SharedWithMe)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var consents = await _context.PatientDoctorConsents
                .AsNoTracking()
                .Where(c => c.DoctorId == currentUser.Id && c.IsActive)
                .Include(c => c.Patient)
                .Include(c => c.ConsentItemAccesses)
                .OrderByDescending(c => c.GrantedDate)
                .ToListAsync();

            var model = new SharedWithMeViewModel
            {
                Patients = consents
                    .Where(c => c.Patient != null)
                    .GroupBy(c => c.PatientID)
                    .Select(g =>
                    {
                        var latest = g.OrderByDescending(c => c.GrantedDate).First();
                        var totalItems = g.Sum(c => c.ConsentItemAccesses?.Count ?? 0);

                        return new SharedPatientViewModel
                        {
                            PatientID = latest.Patient!.PatientID,
                            Name = latest.Patient.Name,
                            Surname = latest.Patient.Surname,
                            IDNumber = latest.Patient.IDNumber,
                            Email = latest.Patient.Email ?? "",
                            SharedItemCount = totalItems,
                            LastGrantedDate = latest.GrantedDate
                        };
                    })
                    .OrderByDescending(p => p.LastGrantedDate)
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // RESULTS PASSWORD GATE
        // =========================================================

        private const string ResultsAccessSessionKey = "DoctorResultsAccess";
        private const string ResultsRequiredPassword = "Password!123";

        [HttpGet]
        public IActionResult ResultsAccess(int? requestId, string? returnUrl)
        {
            // Already authenticated this session? Go straight through.
            if (HttpContext.Session.GetString(ResultsAccessSessionKey) == "Granted")
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction(nameof(PatientSharedResults), new { requestId = requestId ?? 0 });
            }

            ViewBag.RequestId = requestId ?? 0;
            ViewBag.ReturnUrl = returnUrl;
            return View("ResultsPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResultsAccess(int? requestId, string? returnUrl, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Please enter the access password.");
                ViewBag.RequestId = requestId ?? 0;
                ViewBag.ReturnUrl = returnUrl;
                return View("ResultsPassword");
            }

            if (password != ResultsRequiredPassword)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password. Please try again.");
                ViewBag.RequestId = requestId ?? 0;
                ViewBag.ReturnUrl = returnUrl;
                return View("ResultsPassword");
            }

            HttpContext.Session.SetString(ResultsAccessSessionKey, "Granted");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(PatientSharedResults), new { requestId = requestId ?? 0 });
        }

        // =========================================================
        // PATIENT SHARED RESULTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> PatientSharedResults(int patientId)
        {
            // =========================================================
            // PASSWORD GATE CHECK
            // =========================================================
            if (HttpContext.Session.GetString(ResultsAccessSessionKey) != "Granted")
            {
                return RedirectToAction(nameof(ResultsAccess), new
                {
                    returnUrl = Url.Action("PatientSharedResults", new { patientId })
                });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
            {
                return NotFound();
            }

            var hasAccess = await _context.PatientDoctorConsents
                .AsNoTracking()
                .AnyAsync(c =>
                    c.PatientID == patientId &&
                    c.DoctorId == currentUser.Id &&
                    c.IsActive);

            if (!hasAccess)
            {
                return Forbid();
            }

            var grantedItems = await _context.ConsentItemAccesses
                .AsNoTracking()
                .Where(a =>
                    a.Consent != null &&
                    a.Consent.PatientID == patientId &&
                    a.Consent.DoctorId == currentUser.Id &&
                    a.Consent.IsActive)
                .Include(a => a.TestRequestItem)
                    .ThenInclude(i => i!.TestType)
                .Include(a => a.TestRequestItem)
                    .ThenInclude(i => i!.TestRequest)
                .Include(a => a.TestRequestItem)
                    .ThenInclude(i => i!.TestResults)
                .ToListAsync();

            var results = grantedItems
                .Where(a => a.TestRequestItem != null)
                .Select(a =>
                {
                    var item = a.TestRequestItem!;
                    var request = item.TestRequest;
                    var testType = item.TestType;

                    var result = item.TestResults?
                        .Where(r => !string.IsNullOrWhiteSpace(r.ResultValue))
                        .OrderByDescending(r => r.DateCaptured)
                        .FirstOrDefault();

                    return new SharedResultViewModel
                    {
                        RequestID = request?.RequestId ?? 0,
                        RequestDate = request?.RequestDate ?? DateTime.MinValue,
                        Urgency = request?.Urgency ?? "Routine",
                        RequestStatus = request?.Status ?? "",

                        TestRequestItemID = item.TestRequestItemId,
                        TestName = testType?.Name ?? "Unknown Test",
                        Category = testType?.Category ?? "",

                        ResultValue = result?.ResultValue ?? "",
                        Unit = result?.Units ?? "",
                        IsAbnormal = result?.IsAbnormal ?? false,
                        DateCaptured = result?.DateCaptured,
                        TechnicianNotes = result?.Comments ?? ""
                    };
                })
                .OrderByDescending(r => r.RequestDate)
                .ThenBy(r => r.TestName)
                .ToList();

            var model = new PatientSharedResultsViewModel
            {
                PatientID = patient.PatientID,
                Name = patient.Name,
                Surname = patient.Surname,
                IDNumber = patient.IDNumber,
                Email = patient.Email ?? "",
                Results = results
            };

            return View(model);
        }

        // =========================================================
        // CREATE PATIENT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreatePatient(string? idNumber)
        {
            var vm = new PatientCreateViewModel();

            if (!string.IsNullOrWhiteSpace(idNumber))
            {
                vm.IDNumber = idNumber.Trim();
                TryPopulateDobFromId(vm);
            }

            await PopulateDropdownsAsync(vm);

            return View(vm);
        }

        // =========================================================
        // CREATE PATIENT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientCreateViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(string.Empty,
                    "Patient information could not be received.");

                var blank = new PatientCreateViewModel();
                await PopulateDropdownsAsync(blank);
                return View(blank);
            }

            // ---- SA ID validation ----
            if (!string.IsNullOrWhiteSpace(model.IDNumber) &&
                !IsValidSouthAfricanId(model.IDNumber.Trim(), out string idError))
            {
                ModelState.AddModelError(nameof(model.IDNumber), idError);
            }

            // ---- Address validation ----
            bool validLocation = false;

            if (!string.IsNullOrWhiteSpace(model.Province) &&
                !string.IsNullOrWhiteSpace(model.City) &&
                !string.IsNullOrWhiteSpace(model.Suburb) &&
                !string.IsNullOrWhiteSpace(model.PostalCode))
            {
                validLocation = await _context.SouthAfricanLocations
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Province == model.Province &&
                        x.City == model.City &&
                        x.Suburb == model.Suburb &&
                        x.PostalCode == model.PostalCode);
            }

            if (!validLocation)
            {
                ModelState.AddModelError(nameof(model.PostalCode),
                    "Select a valid province, city, suburb and postal code.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Normalise ----
            string name = model.Name.Trim();
            string surname = model.Surname.Trim();
            string idNumber = model.IDNumber.Trim();
            string cellphone = model.CellphoneNumber.Trim();
            string email = model.Email.Trim().ToLowerInvariant();

            string address1 = model.AddressLine1.Trim();

            string? address2 = string.IsNullOrWhiteSpace(model.AddressLine2)
                ? null
                : model.AddressLine2.Trim();

            string suburb = model.Suburb.Trim();
            string city = model.City.Trim();
            string province = model.Province.Trim();
            string postalCode = model.PostalCode.Trim();

            // ---- Resolve medical selections ----
            var medicalConditions = await ResolveMedicalNameAsync(
                model.SelectedMedicalConditionId, "condition");

            var allergies = await ResolveMedicalNameAsync(
                model.SelectedAllergyId, "allergy");

            var medication = await ResolveMedicalNameAsync(
                model.SelectedMedicationId, "medication");

            if (!medicalConditions.Valid ||
                !allergies.Valid ||
                !medication.Valid)
            {
                ModelState.AddModelError(string.Empty,
                    "One or more medical selections are invalid. Please select them again.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Duplicate checks ----
            if (await _context.Patients.AnyAsync(p => p.IDNumber == idNumber))
            {
                ModelState.AddModelError(nameof(model.IDNumber),
                    "A patient with this South African ID number already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(nameof(model.Email),
                    "An account with this email address already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            if (await _userManager.Users.AnyAsync(u => u.SouthAfricanID == idNumber))
            {
                ModelState.AddModelError(nameof(model.IDNumber),
                    "An account with this South African ID number already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Create Identity account ----
            string generatedPassword = GeneratePatientPassword();

            var user = new LabUser
            {
                UserName = email,
                Email = email,
                FirstName = name,
                LastName = surname,
                Gender = "Not Specified",
                PhoneNumb = cellphone,
                SouthAfricanID = idNumber,
                HPCSANumber = null,
                MustChangePassword = true,
                Timestamp_AccountCreated = DateTime.UtcNow,
                ActivationTokenHash = null,
                ActivationTokenExpiresUtc = null,
                EmailConfirmed = true
            };

            IdentityResult createResult;

            try
            {
                createResult = await _userManager.CreateAsync(user, generatedPassword);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty,
                    "The patient account could not be created. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Assign Patient role ----
            try
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Patient");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    await PopulateDropdownsAsync(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(string.Empty,
                    "The Patient role could not be assigned. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Build full address ----
            string fullAddress = string.Join(", ",
                new[]
                {
                    address1,
                    address2,
                    suburb,
                    city,
                    province,
                    postalCode
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            // ---- Create Patient record ----
            var newPatient = new Patient
            {
                UserId = user.Id,
                Name = name,
                Surname = surname,
                IDNumber = idNumber,
                CellphoneNumber = cellphone,
                DOB = model.DOB,
                Email = email,

                AddressLine1 = address1,
                AddressLine2 = address2,
                Suburb = suburb,
                City = city,
                Province = province,
                PostalCode = postalCode,
                HomeAddress = fullAddress,

                MedicalConditions = medicalConditions.Name,
                Allergies = allergies.Name,
                Medication = medication.Name
            };

            _context.Patients.Add(newPatient);
            _audit.Log("Created", "Patients", $"Registered patient '{name} {surname}'.");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _context.Patients.Remove(newPatient);
                await _context.SaveChangesAsync();

                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(string.Empty,
                    "The patient could not be saved. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // ---- Send login credentials by email ----
            bool emailSent = await TrySendCredentialsEmailAsync(
                user, name, generatedPassword);

            if (emailSent)
            {
                TempData["Success"] =
                    "Patient registered successfully. Login details have been emailed to the patient.";
            }
            else
            {
                TempData["Warning"] =
                    "Patient registered, but the login-details email could not be sent. " +
                    "Use 'Resend credentials' from the patient's record.";
            }

            return RedirectToAction(nameof(ManagePatients));
        }

        // =========================================================
        // RESEND CREDENTIALS (regenerates password)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivation(int patientId)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null) return NotFound();

            var user = await _userManager.FindByIdAsync(patient.UserId);
            if (user == null) return NotFound();

            // Generate a fresh password and reset it.
            string newPassword = GeneratePatientPassword();

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!resetResult.Succeeded)
            {
                TempData["Warning"] = "Could not reset the password. " +
                    string.Join("; ", resetResult.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(ManagePatients),
                    new { searchIDNumber = patient.IDNumber });
            }

            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);

            bool sent = await TrySendCredentialsEmailAsync(user, patient.Name, newPassword);

            TempData[sent ? "Success" : "Warning"] = sent
                ? $"New login details emailed to {user.Email}."
                : "Could not send the login-details email.";

            return RedirectToAction(nameof(ManagePatients),
                new { searchIDNumber = patient.IDNumber });
        }

        // =========================================================
        // ADDRESS DROPDOWN ENDPOINTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetCities(string? province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return Json(new List<string>());

            var cities = await _context.SouthAfricanLocations
                .AsNoTracking()
                .Where(x => x.Province == province)
                .Select(x => x.City)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(cities);
        }

        [HttpGet]
        public async Task<IActionResult> GetSuburbs(string? province, string? city)
        {
            if (string.IsNullOrWhiteSpace(province) ||
                string.IsNullOrWhiteSpace(city))
                return Json(new List<string>());

            var suburbs = await _context.SouthAfricanLocations
                .AsNoTracking()
                .Where(x => x.Province == province && x.City == city)
                .Select(x => x.Suburb)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(suburbs);
        }

        [HttpGet]
        public async Task<IActionResult> GetPostalCodes(
            string? province, string? city, string? suburb)
        {
            if (string.IsNullOrWhiteSpace(province) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(suburb))
                return Json(new List<string>());

            var postalCodes = await _context.SouthAfricanLocations
                .AsNoTracking()
                .Where(x =>
                    x.Province == province &&
                    x.City == city &&
                    x.Suburb == suburb)
                .Select(x => x.PostalCode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(postalCodes);
        }

        // =========================================================
        // POPULATE DROPDOWNS
        // =========================================================

        private async Task PopulateDropdownsAsync(PatientCreateViewModel vm)
        {
            vm.Provinces = new List<SelectListItem>
            {
                new("Eastern Cape", "Eastern Cape"),
                new("Free State", "Free State"),
                new("Gauteng", "Gauteng"),
                new("KwaZulu-Natal", "KwaZulu-Natal"),
                new("Limpopo", "Limpopo"),
                new("Mpumalanga", "Mpumalanga"),
                new("Northern Cape", "Northern Cape"),
                new("North West", "North West"),
                new("Western Cape", "Western Cape")
            };

            foreach (var province in vm.Provinces)
            {
                province.Selected = province.Value == vm.Province;
            }

            var conditions = await _context.MedicalConditions
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.ConditionName)
                .ToListAsync();

            vm.MedicalConditionOptions = conditions
                .Select(x => new MedicalOption
                {
                    Id = x.MedicalConditionId,
                    Name = x.ConditionName,
                    Category = x.Category?.Name ?? "Uncategorised"
                })
                .ToList();

            vm.MedicalConditionCategories = GetCategories(vm.MedicalConditionOptions);

            var allergies = await _context.Allergies
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.AllergyName)
                .ToListAsync();

            vm.AllergyOptions = allergies
                .Select(x => new MedicalOption
                {
                    Id = x.AllergyId,
                    Name = x.AllergyName,
                    Category = x.Category?.Name ?? "Uncategorised"
                })
                .ToList();

            vm.AllergyCategories = GetCategories(vm.AllergyOptions);

            var medications = await _context.Medications
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.MedicationName)
                .ToListAsync();

            vm.MedicationOptions = medications
                .Select(x => new MedicalOption
                {
                    Id = x.MedicationId,
                    Name = x.MedicationName,
                    Category = x.Category?.Name ?? "Uncategorised"
                })
                .ToList();

            vm.MedicationCategories = GetCategories(vm.MedicationOptions);
        }

        private static List<string> GetCategories(IEnumerable<MedicalOption> options)
        {
            return options
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        // =========================================================
        // RESOLVE MEDICAL SELECTION
        // =========================================================

        private async Task<(bool Valid, string Name)> ResolveMedicalNameAsync(
            int? id, string type)
        {
            if (!id.HasValue) return (true, "None");

            switch (type)
            {
                case "condition":
                    var condition = await _context.MedicalConditions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.MedicalConditionId == id.Value && x.IsActive);

                    return condition == null
                        ? (false, "None")
                        : (true, condition.ConditionName);

                case "allergy":
                    var allergy = await _context.Allergies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.AllergyId == id.Value && x.IsActive);

                    return allergy == null
                        ? (false, "None")
                        : (true, allergy.AllergyName);

                case "medication":
                    var medication = await _context.Medications
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.MedicationId == id.Value && x.IsActive);

                    return medication == null
                        ? (false, "None")
                        : (true, medication.MedicationName);

                default:
                    return (false, "None");
            }
        }

        // =========================================================
        // SA ID VALIDATION
        // =========================================================
        private static bool IsValidSouthAfricanId(string id, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                error = "ID number is required.";
                return false;
            }

            if (id.Length != 13 || !id.All(char.IsDigit))
            {
                error = "ID number must be exactly 13 digits (numbers only).";
                return false;
            }

            // Luhn check removed - any 13 digits will pass
            return true;
        }
        private static void TryPopulateDobFromId(PatientCreateViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.IDNumber) ||
                vm.IDNumber.Length != 13 ||
                !vm.IDNumber.All(char.IsDigit))
                return;

            int year2 = int.Parse(vm.IDNumber.Substring(0, 2));
            int month = int.Parse(vm.IDNumber.Substring(2, 2));
            int day = int.Parse(vm.IDNumber.Substring(4, 2));

            int currentYY = DateTime.UtcNow.Year % 100;
            int century = year2 > currentYY ? 1900 : 2000;
            int fullYear = century + year2;

            try
            {
                vm.DOB = new DateTime(fullYear, month, day);
            }
            catch
            {
                // Ignore an invalid date encoded in the ID.
            }
        }

        // =========================================================
        // PASSWORD GENERATOR
        // =========================================================

        private static string GeneratePatientPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // no I, O
            const string lower = "abcdefghijkmnopqrstuvwxyz";  // no l
            const string digits = "23456789";                  // no 0, 1
            const string special = "!@#$%";

            string all = upper + lower + digits + special;

            var chars = new List<char>
            {
                PickRandom(upper),
                PickRandom(lower),
                PickRandom(digits),
                PickRandom(special)
            };

            while (chars.Count < 12)
                chars.Add(PickRandom(all));

            // Fisher–Yates shuffle so the guaranteed characters
            // aren't always in the first four positions.
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static char PickRandom(string source)
        {
            return source[RandomNumberGenerator.GetInt32(source.Length)];
        }

        // =========================================================
        // SEND CREDENTIALS EMAIL
        // =========================================================
        private async Task<bool> TrySendCredentialsEmailAsync(
    LabUser user, string firstName, string plainPassword)
        {
            try
            {
                string loginUrl = Url.Action(
                    "Login",
                    "Account",
                    new { area = "Identity" },
                    Request.Scheme) ?? "#";

                // 1. Build the ViewModel with the password you just generated
                var emailModel = new LabDash.ViewModels.PatientWelcomeEmailViewModel
                {
                    FirstName = firstName,
                    Email = user.Email ?? "",
                    Password = plainPassword,
                    LoginUrl = loginUrl
                };

                // 2. Render the clean HTML from the View
                string body = await _emailRenderer.RenderAsync(
                    "~/Views/Emails/PatientWelcome.cshtml",
                    emailModel,
                    HttpContext);

                // 3. Send it
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Your NMB LAB patient account",
                    body);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CREDENTIALS EMAIL FAILED for {user.Email}: {ex.Message}");
                return false;
            }
        }
        [HttpGet]
        public async Task<IActionResult> DoctorProfile(bool edit = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ViewBag.EditMode = edit;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorProfile(
            string FirstName,
            string LastName,
            string Email,
            string HPCSANumber,
            string EmployeeNumber,
            string PhoneNumb,
            string? Gender,
            string? SouthAfricanID)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(FirstName))
                ModelState.AddModelError("FirstName", "First name is required.");
            if (string.IsNullOrWhiteSpace(LastName))
                ModelState.AddModelError("LastName", "Last name is required.");
            if (string.IsNullOrWhiteSpace(Email))
                ModelState.AddModelError("Email", "Email is required.");
            if (string.IsNullOrWhiteSpace(HPCSANumber))
                ModelState.AddModelError("HPCSANumber", "HPCSA number is required.");
            if (string.IsNullOrWhiteSpace(EmployeeNumber))
                ModelState.AddModelError("EmployeeNumber", "Employee number is required.");
            if (string.IsNullOrWhiteSpace(PhoneNumb))
                ModelState.AddModelError("PhoneNumb", "Phone number is required.");

            string newEmail = (Email ?? "").Trim().ToLowerInvariant();

            if (ModelState.IsValid &&
                !string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(newEmail);
                if (existing != null && existing.Id != user.Id)
                    ModelState.AddModelError("Email", "That email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                user.FirstName = FirstName;
                user.LastName = LastName;
                user.Email = Email;
                user.HPCSANumber = HPCSANumber;
                user.EmployeeNumber = EmployeeNumber;
                user.PhoneNumb = PhoneNumb;
                user.Gender = Gender;
                user.SouthAfricanID = SouthAfricanID;

                ViewBag.EditMode = true;
                return View(user);
            }

            user.FirstName = FirstName.Trim();
            user.LastName = LastName.Trim();
            user.HPCSANumber = HPCSANumber.Trim();
            user.EmployeeNumber = EmployeeNumber.Trim();
            user.PhoneNumb = PhoneNumb.Trim();
            user.Gender = Gender;
            user.SouthAfricanID = string.IsNullOrWhiteSpace(SouthAfricanID)
                ? null : SouthAfricanID.Trim();

            if (!string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = newEmail;
                user.UserName = newEmail;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.EditMode = true;
                return View(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction(nameof(DoctorProfile));
        }
    }
}