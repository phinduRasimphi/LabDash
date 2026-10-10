
using LabDash.Areas.Identity.Data;
using LabDash.Models;
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

        public DoctorController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            SignInManager<LabUser> signInManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // =========================================================
        // MANAGE PATIENTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ManagePatients(
            string? searchIDNumber)
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
                    .FirstOrDefaultAsync(p =>
                        p.IDNumber == searchIDNumber.Trim());

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
        // CREATE PATIENT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreatePatient(
            string? idNumber)
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
        public async Task<IActionResult> CreatePatient(
            PatientCreateViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Patient information could not be received.");

                var blank = new PatientCreateViewModel();
                await PopulateDropdownsAsync(blank);

                return View(blank);
            }

            // Validate the South African ID number.
            if (!string.IsNullOrWhiteSpace(model.IDNumber) &&
                !IsValidSouthAfricanId(
                    model.IDNumber.Trim(),
                    out string idError))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    idError);
            }

            // Validate the selected address against the database.
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
                ModelState.AddModelError(
                    nameof(model.PostalCode),
                    "Select a valid province, city, suburb and postal code.");
            }

            // Reload dropdowns whenever validation fails.
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            string name = model.Name.Trim();
            string surname = model.Surname.Trim();
            string idNumber = model.IDNumber.Trim();
            string cellphone = model.CellphoneNumber.Trim();
            string email = model.Email.Trim().ToLowerInvariant();

            string address1 = model.AddressLine1.Trim();

            string? address2 =
                string.IsNullOrWhiteSpace(model.AddressLine2)
                    ? null
                    : model.AddressLine2.Trim();

            string suburb = model.Suburb.Trim();
            string city = model.City.Trim();
            string province = model.Province.Trim();
            string postalCode = model.PostalCode.Trim();

            // Resolve medical selections from the database.
            var medicalConditions = await ResolveMedicalNameAsync(
                model.SelectedMedicalConditionId,
                "condition");

            var allergies = await ResolveMedicalNameAsync(
                model.SelectedAllergyId,
                "allergy");

            var medication = await ResolveMedicalNameAsync(
                model.SelectedMedicationId,
                "medication");

            if (!medicalConditions.Valid ||
                !allergies.Valid ||
                !medication.Valid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more medical selections are invalid. Please select them again.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Prevent duplicate patient ID numbers.
            if (await _context.Patients.AnyAsync(
                p => p.IDNumber == idNumber))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "A patient with this South African ID number already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Prevent duplicate email accounts.
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email address already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Prevent the same ID being attached to another account.
            if (await _userManager.Users.AnyAsync(
                u => u.SouthAfricanID == idNumber))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "An account with this South African ID number already exists.");

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Create the Identity account.
            string generatedPassword = GenerateTemporaryPassword();

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
                Timestamp_AccountCreated = DateTime.UtcNow
            };

            IdentityResult createResult;

            try
            {
                createResult = await _userManager.CreateAsync(
                    user,
                    generatedPassword);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The patient account could not be created. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Assign the Patient role.
            try
            {
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Patient");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    await PopulateDropdownsAsync(model);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "The Patient role could not be assigned. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Build the full address.
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

            // Create the patient database record.
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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _context.Patients.Remove(newPatient);
                await _context.SaveChangesAsync();

                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "The patient could not be saved. " +
                    (ex.InnerException?.Message ?? ex.Message));

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Send the welcome email.
            bool emailSent = false;

            try
            {
                string? loginUrl = Url.Action(
                    "Login",
                    "Account",
                    new { area = "Identity" },
                    Request.Scheme);

                string safeName =
                    System.Net.WebUtility.HtmlEncode(name);

                string safeEmail =
                    System.Net.WebUtility.HtmlEncode(email);

                string safePassword =
                    System.Net.WebUtility.HtmlEncode(generatedPassword);

                string safeLoginUrl =
                    System.Net.WebUtility.HtmlEncode(loginUrl ?? "#");

                string emailBody = $@"
                    <h2>Welcome to NMB LAB</h2>
                    <p>Hi {safeName},</p>
                    <p>Your patient account has been created.</p>
                    <p><strong>Username:</strong> {safeEmail}</p>
                    <p><strong>Temporary Password:</strong> {safePassword}</p>
                    <p>You must change your password at first login.</p>
                    <p><a href=""{safeLoginUrl}"">Login here</a></p>
                    <p>Regards,<br/>NMB Haematology Laboratory</p>";

                await _emailSender.SendEmailAsync(
                    email,
                    "Your NMB LAB Patient Account",
                    emailBody);

                emailSent = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"EMAIL FAILED: {ex.Message}");
            }

            if (emailSent)
            {
                TempData["Success"] =
                    "Patient registered successfully. Login details have been emailed.";
            }
            else
            {
                TempData["Warning"] =
                    "Patient registered, but the welcome email could not be sent. " +
                    "The login details were not delivered.";
            }

            return RedirectToAction(nameof(ManagePatients));
        }

        // =========================================================
        // ADDRESS DROPDOWN ENDPOINTS
        // Province -> City -> Suburb -> Postal Code
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetCities(string? province)
        {
            if (string.IsNullOrWhiteSpace(province))
            {
                return Json(new List<string>());
            }

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
        public async Task<IActionResult> GetSuburbs(
            string? province,
            string? city)
        {
            if (string.IsNullOrWhiteSpace(province) ||
                string.IsNullOrWhiteSpace(city))
            {
                return Json(new List<string>());
            }

            var suburbs = await _context.SouthAfricanLocations
                .AsNoTracking()
                .Where(x =>
                    x.Province == province &&
                    x.City == city)
                .Select(x => x.Suburb)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(suburbs);
        }

        [HttpGet]
        public async Task<IActionResult> GetPostalCodes(
            string? province,
            string? city,
            string? suburb)
        {
            if (string.IsNullOrWhiteSpace(province) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(suburb))
            {
                return Json(new List<string>());
            }

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

        private async Task PopulateDropdownsAsync(
            PatientCreateViewModel vm)
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

            vm.MedicalConditionCategories =
                GetCategories(vm.MedicalConditionOptions);

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

        private static List<string> GetCategories(
            IEnumerable<MedicalOption> options)
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

        private async Task<(bool Valid, string Name)>
            ResolveMedicalNameAsync(int? id, string type)
        {
            if (!id.HasValue)
            {
                return (true, "None");
            }

            switch (type)
            {
                case "condition":
                    var condition = await _context.MedicalConditions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.MedicalConditionId == id.Value &&
                            x.IsActive);

                    return condition == null
                        ? (false, "None")
                        : (true, condition.ConditionName);

                case "allergy":
                    var allergy = await _context.Allergies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.AllergyId == id.Value &&
                            x.IsActive);

                    return allergy == null
                        ? (false, "None")
                        : (true, allergy.AllergyName);

                case "medication":
                    var medication = await _context.Medications
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.MedicationId == id.Value &&
                            x.IsActive);

                    return medication == null
                        ? (false, "None")
                        : (true, medication.MedicationName);

                default:
                    return (false, "None");
            }
        }

        // =========================================================
        // SOUTH AFRICAN ID VALIDATION
        // =========================================================

        private static bool IsValidSouthAfricanId(
            string id,
            out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(id) ||
                id.Length != 13 ||
                !id.All(char.IsDigit))
            {
                error = "ID number must be exactly 13 digits.";
                return false;
            }

            if (!int.TryParse(id.Substring(2, 2), out int month) ||
                !int.TryParse(id.Substring(4, 2), out int day))
            {
                error = "ID number contains an invalid birth date.";
                return false;
            }

            int year2 = int.Parse(id.Substring(0, 2));
            int currentYY = DateTime.UtcNow.Year % 100;
            int century = year2 > currentYY ? 1900 : 2000;
            int fullYear = century + year2;

            if (month < 1 || month > 12)
            {
                error = "ID number contains an invalid birth month.";
                return false;
            }

            if (day < 1 ||
                day > DateTime.DaysInMonth(fullYear, month))
            {
                error = "ID number contains an invalid birth day.";
                return false;
            }

            int sum = 0;
            bool alternate = false;

            for (int i = id.Length - 1; i >= 0; i--)
            {
                int n = id[i] - '0';

                if (alternate)
                {
                    n *= 2;

                    if (n > 9)
                    {
                        n -= 9;
                    }
                }

                sum += n;
                alternate = !alternate;
            }

            if (sum % 10 != 0)
            {
                error = "ID number failed the checksum validation.";
                return false;
            }

            return true;
        }

        private static void TryPopulateDobFromId(
            PatientCreateViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.IDNumber) ||
                vm.IDNumber.Length != 13 ||
                !vm.IDNumber.All(char.IsDigit))
            {
                return;
            }

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
        // TEMPORARY PASSWORD GENERATION
        // =========================================================

        private static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string numbers = "23456789";
            const string special = "!@#$%";

            const string all =
                uppercase + lowercase + numbers + special;

            var password = new List<char>
            {
                GetRandomCharacter(uppercase),
                GetRandomCharacter(lowercase),
                GetRandomCharacter(numbers),
                GetRandomCharacter(special)
            };

            while (password.Count < 12)
            {
                password.Add(GetRandomCharacter(all));
            }

            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }

        private static char GetRandomCharacter(string characters)
        {
            int index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }

        // =========================================================
        // DOCTOR PROFILE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DoctorProfile(bool edit = false)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.EditMode = edit;
            return View(user);
        }
        [HttpGet]
        public async Task<IActionResult> DebugLocations(string province = "Gauteng")
        {
            var connection = _context.Database.GetDbConnection();
            var dbName = connection.Database;
            var server = connection.DataSource;

            var total = await _context.SouthAfricanLocations.CountAsync();

            var distinctProvinces = await _context.SouthAfricanLocations
                .Select(x => x.Province)
                .Distinct()
                .ToListAsync();

            var matching = await _context.SouthAfricanLocations
                .Where(x => x.Province == province)
                .Select(x => new { x.Province, x.City, x.Suburb, x.PostalCode })
                .Take(20)
                .ToListAsync();

            return Json(new
            {
                ConnectedServer = server,
                ConnectedDatabase = dbName,
                SearchedProvince = province,
                TotalRowsInTable = total,
                DistinctProvincesSeen = distinctProvinces,
                MatchingRows = matching
            });
        }

        // =========================================================
        // DOCTOR PROFILE - POST
        // =========================================================

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

            if (user == null)
            {
                return NotFound();
            }

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
                !string.Equals(newEmail, user.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(newEmail);

                if (existing != null && existing.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "That email is already in use.");
                }
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
                ? null
                : SouthAfricanID.Trim();

            if (!string.Equals(newEmail, user.Email,
                StringComparison.OrdinalIgnoreCase))
            {
                user.Email = newEmail;
                user.UserName = newEmail;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ViewBag.EditMode = true;
                return View(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction(nameof(DoctorProfile));
        }
    }
}
