using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> ManagePatients(string? searchIDNumber)
        {
            var vm = new ManagePatientsViewModel { SearchIDNumber = searchIDNumber };

            if (!string.IsNullOrWhiteSpace(searchIDNumber))
            {
                vm.HasSearched = true;
                var patient = await _context.Patients
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
        // CREATE PATIENT — GET
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
        // CREATE PATIENT — POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientCreateViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(string.Empty, "Patient information could not be received.");
                var blank = new PatientCreateViewModel();
                await PopulateDropdownsAsync(blank);
                return View(blank);
            }

            // SA ID validation (Luhn + embedded date)
            if (!string.IsNullOrWhiteSpace(model.IDNumber) &&
                !IsValidSouthAfricanId(model.IDNumber, out string idError))
            {
                ModelState.AddModelError(nameof(model.IDNumber), idError);
            }

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
            string? address2 = string.IsNullOrWhiteSpace(model.AddressLine2) ? null : model.AddressLine2.Trim();
            string? suburb = string.IsNullOrWhiteSpace(model.Suburb) ? null : model.Suburb.Trim();
            string? city = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            string province = model.Province.Trim();
            string? postalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode.Trim();

            // Resolve medical lookups
            string medicalConditions = await ResolveNameAsync(model.SelectedMedicalConditionId, "condition");
            string allergies = await ResolveNameAsync(model.SelectedAllergyId, "allergy");
            string medication = await ResolveNameAsync(model.SelectedMedicationId, "medication");

            // Duplicate checks
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

            // Create Identity user
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
                createResult = await _userManager.CreateAsync(user, generatedPassword);
            }
            catch (Exception ex)
            {
                string error = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, "The patient account could not be created. " + error);
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Assign Patient role
            try
            {
                await _userManager.AddToRoleAsync(user, "Patient");
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                string error = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, "The Patient role could not be assigned. " + error);
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Create Patient entity
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
                HomeAddress = address1,
                MedicalConditions = medicalConditions,
                Allergies = allergies,
                Medication = medication
            };

            _context.Patients.Add(newPatient);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                string error = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, "The patient could not be saved. " + error);
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            // Send welcome email — failure is non-blocking
            bool emailSent = false;

            try
            {
                string? loginUrl = Url.Action("Login", "Account", new { area = "Identity" }, Request.Scheme);
                string emailBody = $@"
                    <h2>Welcome to NMB LAB</h2>
                    <p>Hi {name},</p>
                    <p>Your patient account has been created.</p>
                    <p>
                        <strong>Username:</strong> {email}<br/>
                        <strong>Temporary Password:</strong> {generatedPassword}
                    </p>
                    <p>You will be required to change your password at first login.</p>
                    <p><a href='{loginUrl}'>Login here</a></p>
                    <p>Regards,<br/>NMB Haematology Laboratory</p>";

                await _emailSender.SendEmailAsync(email, "Your NMB LAB Patient Account", emailBody);
                emailSent = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EMAIL FAILED: {ex.Message}");
            }

            if (emailSent)
            {
                TempData["Success"] = "Patient registered successfully. Login details have been emailed.";
            }
            else
            {
                TempData["Warning"] =
                    $"Patient registered, but the email could not be sent. " +
                    $"Give them this temporary password: {generatedPassword}";
            }

            return RedirectToAction(nameof(ManagePatients));
        }

        // =========================================================
        // HELPERS — DROPDOWNS
        // =========================================================
        private async Task PopulateDropdownsAsync(PatientCreateViewModel vm)
        {
            // Provinces (fixed list)
            vm.Provinces = new List<SelectOption>
            {
                new() { Value = "Eastern Cape",  Text = "Eastern Cape" },
                new() { Value = "Free State",    Text = "Free State" },
                new() { Value = "Gauteng",       Text = "Gauteng" },
                new() { Value = "KwaZulu-Natal", Text = "KwaZulu-Natal" },
                new() { Value = "Limpopo",       Text = "Limpopo" },
                new() { Value = "Mpumalanga",    Text = "Mpumalanga" },
                new() { Value = "Northern Cape", Text = "Northern Cape" },
                new() { Value = "North West",    Text = "North West" },
                new() { Value = "Western Cape",  Text = "Western Cape" }
            };

            vm.Cities = new List<SelectOption>();
            vm.Suburbs = new List<SelectOption>();

            // ----- Medical Conditions -----
            try
            {
                vm.MedicalConditionOptions = await _context.MedicalConditions
                    .Include(m => m.Category)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.ConditionName)
                    .Select(m => new MedicalOption
                    {
                        Id = m.MedicalConditionId,
                        Name = m.ConditionName,
                        Category = m.Category != null ? m.Category.Name : "Uncategorised"
                    })
                    .ToListAsync();

                vm.MedicalConditionCategories = vm.MedicalConditionOptions
                    .Select(o => o.Category)
                    .Distinct()
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                vm.MedicalConditionOptions = new();
                vm.MedicalConditionCategories = new();
            }

            // ----- Allergies -----
            try
            {
                vm.AllergyOptions = await _context.Allergies
                    .Include(a => a.Category)
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.AllergyName)
                    .Select(a => new MedicalOption
                    {
                        Id = a.AllergyId,
                        Name = a.AllergyName,
                        Category = a.Category != null ? a.Category.Name : "Uncategorised"
                    })
                    .ToListAsync();

                vm.AllergyCategories = vm.AllergyOptions
                    .Select(o => o.Category)
                    .Distinct()
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                vm.AllergyOptions = new();
                vm.AllergyCategories = new();
            }

            // ----- Medications -----
            try
            {
                vm.MedicationOptions = await _context.Medications
                    .Include(m => m.Category)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicationName)
                    .Select(m => new MedicalOption
                    {
                        Id = m.MedicationId,
                        Name = m.MedicationName,
                        Category = m.Category != null ? m.Category.Name : "Uncategorised"
                    })
                    .ToListAsync();

                vm.MedicationCategories = vm.MedicationOptions
                    .Select(o => o.Category)
                    .Distinct()
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                vm.MedicationOptions = new();
                vm.MedicationCategories = new();
            }
        }

        // =========================================================
        // HELPERS — RESOLVE MEDICAL NAME
        // =========================================================
        private async Task<string> ResolveNameAsync(int? id, string type)
        {
            if (id == null) return "None";

            switch (type)
            {
                case "condition":
                    var c = await _context.MedicalConditions.FindAsync(id.Value);
                    return c?.ConditionName ?? "None";
                case "allergy":
                    var a = await _context.Allergies.FindAsync(id.Value);
                    return a?.AllergyName ?? "None";
                case "medication":
                    var m = await _context.Medications.FindAsync(id.Value);
                    return m?.MedicationName ?? "None";
                default:
                    return "None";
            }
        }

        // =========================================================
        // HELPERS — SA ID VALIDATION + DOB EXTRACTION
        // =========================================================
        private static bool IsValidSouthAfricanId(string id, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(id) || id.Length != 13 || !id.All(char.IsDigit))
            {
                error = "ID number must be exactly 13 digits.";
                return false;
            }

            string yy = id.Substring(0, 2);
            string mm = id.Substring(2, 2);
            string dd = id.Substring(4, 2);

            int year2 = int.Parse(yy);
            int month = int.Parse(mm);
            int day = int.Parse(dd);

            int currentYY = DateTime.UtcNow.Year % 100;
            int century = year2 > currentYY ? 1900 : 2000;
            int fullYear = century + year2;

            if (month < 1 || month > 12)
            {
                error = "ID number contains an invalid birth month.";
                return false;
            }

            if (day < 1 || day > DateTime.DaysInMonth(fullYear, month))
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
                    if (n > 9) n -= 9;
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

        private static void TryPopulateDobFromId(PatientCreateViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.IDNumber) || vm.IDNumber.Length < 6) return;

            string yy = vm.IDNumber.Substring(0, 2);
            string mm = vm.IDNumber.Substring(2, 2);
            string dd = vm.IDNumber.Substring(4, 2);

            if (!int.TryParse(yy, out int year2) ||
                !int.TryParse(mm, out int month) ||
                !int.TryParse(dd, out int day)) return;

            int currentYY = DateTime.UtcNow.Year % 100;
            int century = year2 > currentYY ? 1900 : 2000;
            int fullYear = century + year2;

            try
            {
                vm.DOB = new DateTime(fullYear, month, day);
            }
            catch { /* invalid date — leave DOB untouched */ }
        }

        // =========================================================
        // HELPERS — PASSWORD GENERATION
        // =========================================================
        private string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string numbers = "23456789";
            const string special = "!@#$%";

            var password = new List<char>
            {
                GetRandomCharacter(uppercase),
                GetRandomCharacter(lowercase),
                GetRandomCharacter(numbers),
                GetRandomCharacter(special)
            };

            const string allCharacters = uppercase + lowercase + numbers + special;

            while (password.Count < 12)
                password.Add(GetRandomCharacter(allCharacters));

            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }

        private char GetRandomCharacter(string characters)
        {
            if (string.IsNullOrEmpty(characters))
                throw new ArgumentException("Character set cannot be empty.", nameof(characters));

            int index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }
    }
}