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
        // GET: /Doctor/ManagePatients
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

                string cleanID = searchIDNumber.Trim();

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IDNumber == cleanID);

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
                        HomeAddress = patient.HomeAddress,
                        MedicalConditions = patient.MedicalConditions,
                        Allergies = patient.Allergies,
                        Medication = patient.Medication
                    };
                }
            }

            return View(vm);
        }

        // =========================================================
        // MY PATIENTS
        // GET: /Doctor/MyPatients
        // =========================================================

        [HttpGet]
        public IActionResult MyPatients()
        {
            if (HttpContext.Session.GetString("DoctorPatientAccess") == "Granted")
            {
                return RedirectToAction(nameof(ViewMyPatients));
            }

            return View("MyPatientsPassword");
        }

        // =========================================================
        // MY PATIENTS - PASSWORD CHECK
        // POST: /Doctor/MyPatientsAccess
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MyPatientsAccess(string password)
        {
            const string requiredPassword = "Password!123";

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please enter the access password."
                );

                return View("MyPatientsPassword");
            }

            if (password != requiredPassword)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Incorrect password. Please check the password and try again."
                );

                return View("MyPatientsPassword");
            }

            HttpContext.Session.SetString("DoctorPatientAccess", "Granted");

            return RedirectToAction(nameof(ViewMyPatients));
        }

        // =========================================================
        // VIEW MY PATIENTS
        // GET: /Doctor/ViewMyPatients
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewMyPatients()
        {
            if (HttpContext.Session.GetString("DoctorPatientAccess") != "Granted")
            {
                return RedirectToAction(nameof(MyPatients));
            }

            var patients = await _context.Patients
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View("MyPatients", patients);
        }

        // =========================================================
        // CREATE PATIENT - GET
        // GET: /Doctor/CreatePatient
        // =========================================================

        [HttpGet]
        public IActionResult CreatePatient(string? idNumber)
        {
            var vm = new PatientCreateViewModel();

            if (!string.IsNullOrWhiteSpace(idNumber))
            {
                vm.IDNumber = idNumber.Trim();
            }

            return View(vm);
        }

        // =========================================================
        // CREATE PATIENT - POST
        // POST: /Doctor/CreatePatient
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientCreateViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Patient information could not be received."
                );

                return View(new PatientCreateViewModel());
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Patient name is required.");

            if (string.IsNullOrWhiteSpace(model.Surname))
                ModelState.AddModelError(nameof(model.Surname), "Patient surname is required.");

            if (string.IsNullOrWhiteSpace(model.IDNumber))
                ModelState.AddModelError(nameof(model.IDNumber), "South African ID number is required.");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email address is required.");

            if (string.IsNullOrWhiteSpace(model.CellphoneNumber))
                ModelState.AddModelError(nameof(model.CellphoneNumber), "Cellphone number is required.");

            if (string.IsNullOrWhiteSpace(model.HomeAddress))
                ModelState.AddModelError(nameof(model.HomeAddress), "Home address is required.");

            if (!ModelState.IsValid)
                return View(model);

            string name = model.Name?.Trim() ?? string.Empty;
            string surname = model.Surname?.Trim() ?? string.Empty;
            string idNumber = model.IDNumber?.Trim() ?? string.Empty;
            string cellphone = model.CellphoneNumber?.Trim() ?? string.Empty;
            string email = model.Email?.Trim().ToLower() ?? string.Empty;
            string homeAddress = model.HomeAddress?.Trim() ?? string.Empty;

            string medicalConditions = string.IsNullOrWhiteSpace(model.MedicalConditions)
                ? "None"
                : model.MedicalConditions.Trim();

            string allergies = string.IsNullOrWhiteSpace(model.Allergies)
                ? "None"
                : model.Allergies.Trim();

            string medication = string.IsNullOrWhiteSpace(model.Medication)
                ? "None"
                : model.Medication.Trim();

            bool idExists = await _context.Patients
                .AnyAsync(p => p.IDNumber == idNumber);

            if (idExists)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "A patient with this South African ID number already exists."
                );

                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email address already exists."
                );

                return View(model);
            }

            var existingIdUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.SouthAfricanID == idNumber);

            if (existingIdUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "An account with this South African ID number already exists."
                );

                return View(model);
            }

            string generatedPassword = GenerateTemporaryPassword();
            string patientEmployeeNumber = await GeneratePatientEmployeeNumberAsync();

            var user = new LabUser
            {
                UserName = email,
                Email = email,
                FirstName = name,
                LastName = surname,
                Gender = "Not Specified",
                PhoneNumb = cellphone,
                SouthAfricanID = idNumber,
                EmployeeNumber = patientEmployeeNumber,
                HPCSANumber = null,
                MustChangePassword = true
            };

            IdentityResult createResult;

            try
            {
                createResult = await _userManager.CreateAsync(user, generatedPassword);
            }
            catch (Exception ex)
            {
                string error = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient account could not be created. " + error
                );

                return View(model);
            }

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            IdentityResult roleResult;

            try
            {
                roleResult = await _userManager.AddToRoleAsync(user, "Patient");
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                string error = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The Patient role could not be assigned. " + error
                );

                return View(model);
            }

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            var patient = new Patient
            {
                UserId = user.Id,
                Name = name,
                Surname = surname,
                IDNumber = idNumber,
                CellphoneNumber = cellphone,
                DOB = model.DOB,
                Email = email,
                HomeAddress = homeAddress,
                MedicalConditions = medicalConditions,
                Allergies = allergies,
                Medication = medication
            };

            _context.Patients.Add(patient);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                await _userManager.DeleteAsync(user);

                string error = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient account could not be created. " + error
                );

                return View(model);
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                string error = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while saving the patient. " + error
                );

                return View(model);
            }

            bool emailSent = false;

            try
            {
                string? loginUrl = Url.Action("Login", "Account", null, Request.Scheme);

                string emailBody =
                    $@"
                    <html>
                    <body style='font-family:Arial,sans-serif;'>

                        <h2 style='color:#126b65;'>
                            Welcome to NMB LAB
                        </h2>

                        <p>Hi {name},</p>

                        <p>
                            Your patient account has been
                            successfully created.
                        </p>

                        <p>Your login details are:</p>

                        <table cellpadding='8' cellspacing='0'
                               style='border-collapse:collapse;'>
                            <tr>
                                <td><strong>Username:</strong></td>
                                <td>{email}</td>
                            </tr>
                            <tr>
                                <td><strong>Temporary Password:</strong></td>
                                <td>{generatedPassword}</td>
                            </tr>
                        </table>

                        <p>
                            You will be required to change
                            your password when you first log in.
                        </p>

                        <p>
                            <a href='{loginUrl}'
                               style='
                                    display:inline-block;
                                    padding:10px 18px;
                                    background:#126b65;
                                    color:white;
                                    text-decoration:none;
                                    border-radius:6px;
                               '>
                                Login to NMB LAB
                            </a>
                        </p>

                        <p>
                            If you did not expect this account,
                            please contact the laboratory administrator.
                        </p>

                        <p>
                            Regards,<br />
                            <strong>NMB Haematology Laboratory</strong>
                        </p>

                    </body>
                    </html>";

                await _emailSender.SendEmailAsync(
                    email,
                    "Your NMB LAB Patient Account",
                    emailBody
                );

                emailSent = true;
            }
            catch
            {
                // Patient remains registered even if email fails.
            }

            if (emailSent)
            {
                TempData["Success"] =
                    "Patient registered successfully. " +
                    "Login details have been emailed to the patient.";
            }
            else
            {
                TempData["Success"] =
                    "Patient registered successfully. " +
                    "However, the login email could not be sent.";
            }

            return RedirectToAction(nameof(ManagePatients));
        }

        // =========================================================
        // EDIT PATIENT
        // GET: /Doctor/EditPatient/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditPatient(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == id);

            if (patient == null) return NotFound();

            var vm = new PatientCreateViewModel
            {
                Name = patient.Name,
                Surname = patient.Surname,
                IDNumber = patient.IDNumber,
                DOB = patient.DOB,
                CellphoneNumber = patient.CellphoneNumber,
                Email = patient.Email,
                HomeAddress = patient.HomeAddress,
                MedicalConditions = patient.MedicalConditions,
                Allergies = patient.Allergies,
                Medication = patient.Medication
            };

            ViewBag.PatientID = patient.PatientID;

            return View(vm);
        }

        // =========================================================
        // UPDATE PATIENT
        // POST: /Doctor/UpdatePatient
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatient(PatientDetailsViewModel model)
        {
            if (model == null)
            {
                TempData["Error"] = "The patient information could not be received.";
                return RedirectToAction(nameof(ManagePatients));
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Patient name is required.");

            if (string.IsNullOrWhiteSpace(model.Surname))
                ModelState.AddModelError(nameof(model.Surname), "Patient surname is required.");

            if (string.IsNullOrWhiteSpace(model.IDNumber))
                ModelState.AddModelError(nameof(model.IDNumber), "South African ID number is required.");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email address is required.");

            if (string.IsNullOrWhiteSpace(model.CellphoneNumber))
                ModelState.AddModelError(nameof(model.CellphoneNumber), "Cellphone number is required.");

            if (string.IsNullOrWhiteSpace(model.HomeAddress))
                ModelState.AddModelError(nameof(model.HomeAddress), "Home address is required.");

            if (!ModelState.IsValid)
                return View("EditPatient", model);

            string name = model.Name?.Trim() ?? string.Empty;
            string surname = model.Surname?.Trim() ?? string.Empty;
            string idNumber = model.IDNumber?.Trim() ?? string.Empty;
            string cellphone = model.CellphoneNumber?.Trim() ?? string.Empty;
            string email = model.Email?.Trim().ToLower() ?? string.Empty;
            string homeAddress = model.HomeAddress?.Trim() ?? string.Empty;

            string medicalConditions = string.IsNullOrWhiteSpace(model.MedicalConditions)
                ? "None"
                : model.MedicalConditions.Trim();

            string allergies = string.IsNullOrWhiteSpace(model.Allergies)
                ? "None"
                : model.Allergies.Trim();

            string medication = string.IsNullOrWhiteSpace(model.Medication)
                ? "None"
                : model.Medication.Trim();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == model.PatientID);

            if (patient == null)
            {
                TempData["Error"] = "The patient record could not be found.";
                return RedirectToAction(nameof(ManagePatients));
            }

            bool duplicatePatientID = await _context.Patients
                .AnyAsync(p => p.IDNumber == idNumber && p.PatientID != model.PatientID);

            if (duplicatePatientID)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "Another patient already uses this South African ID number."
                );

                return View("EditPatient", model);
            }

            LabUser? user = null;

            if (!string.IsNullOrWhiteSpace(patient.UserId))
            {
                user = await _userManager.FindByIdAsync(patient.UserId);
            }

            if (user != null)
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(email);

                if (existingEmailUser != null && existingEmailUser.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Another account already uses this email address."
                    );

                    return View("EditPatient", model);
                }

                var existingIdUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.SouthAfricanID == idNumber && u.Id != user.Id);

                if (existingIdUser != null)
                {
                    ModelState.AddModelError(
                        nameof(model.IDNumber),
                        "Another account already uses this South African ID number."
                    );

                    return View("EditPatient", model);
                }
            }

            patient.Name = name;
            patient.Surname = surname;
            patient.IDNumber = idNumber;
            patient.DOB = model.DOB;
            patient.CellphoneNumber = cellphone;
            patient.Email = email;
            patient.HomeAddress = homeAddress;
            patient.MedicalConditions = medicalConditions;
            patient.Allergies = allergies;
            patient.Medication = medication;

            if (user != null)
            {
                user.FirstName = name;
                user.LastName = surname;
                user.PhoneNumb = cellphone;
                user.SouthAfricanID = idNumber;
                user.Email = email;
                user.UserName = email;

                var updateUserResult = await _userManager.UpdateAsync(user);

                if (!updateUserResult.Succeeded)
                {
                    foreach (var error in updateUserResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return View("EditPatient", model);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                string databaseError = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient information could not be saved. " + databaseError
                );

                return View("EditPatient", model);
            }
            catch (Exception ex)
            {
                string error = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while saving the patient. " + error
                );

                return View("EditPatient", model);
            }

            TempData["Success"] = "Patient record updated successfully.";

            return RedirectToAction(nameof(ManagePatients));
        }

        // =========================================================
        // GENERATE UNIQUE PATIENT EMPLOYEE NUMBER
        // =========================================================

        private async Task<string> GeneratePatientEmployeeNumberAsync()
        {
            string employeeNumber;

            do
            {
                employeeNumber =
                    "PAT-" +
                    RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            }
            while (
                await _userManager.Users.AnyAsync(
                    u => u.EmployeeNumber == employeeNumber
                )
            );

            return employeeNumber;
        }

        // =========================================================
        // GENERATE SECURE TEMPORARY PASSWORD
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

            const string allCharacters =
                uppercase + lowercase + numbers + special;

            while (password.Count < 12)
            {
                password.Add(GetRandomCharacter(allCharacters));
            }

            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);

                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }

        // =========================================================
        // GET RANDOM CHARACTER
        // =========================================================

        private char GetRandomCharacter(string characters)
        {
            if (string.IsNullOrEmpty(characters))
            {
                throw new ArgumentException(
                    "Character set cannot be empty.",
                    nameof(characters)
                );
            }

            int index = RandomNumberGenerator.GetInt32(characters.Length);

            return characters[index];
        }

        // =========================================================
        // DOCTOR PROFILE
        // GET: /Doctor/DoctorProfile
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DoctorProfile(bool edit = false)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();

            ViewBag.EditMode = edit;

            return View(user);
        }

        // =========================================================
        // DOCTOR PROFILE
        // POST: /Doctor/DoctorProfile
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorProfile(DoctorProfile model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                ViewBag.EditMode = true;
                return View(user);
            }

            // Clean input
            string firstName = model.FirstName.Trim();
            string lastName = model.LastName.Trim();
            string hpcsa = model.HPCSANumber.Trim().ToUpper();
            string employeeNumber = model.EmployeeNumber.Trim().ToUpper();
            string email = model.Email.Trim().ToLower();
            string phone = model.PhoneNumb.Trim();

            // Unique HPCSA (excluding self)
            bool hpcsaTaken = await _userManager.Users
                .AnyAsync(u => u.HPCSANumber == hpcsa && u.Id != user.Id);

            if (hpcsaTaken)
            {
                ModelState.AddModelError(
                    nameof(model.HPCSANumber),
                    "Another doctor already uses this HPCSA number."
                );

                ViewBag.EditMode = true;
                return View(user);
            }

            // Unique Employee Number (excluding self)
            bool employeeTaken = await _userManager.Users
                .AnyAsync(u => u.EmployeeNumber == employeeNumber && u.Id != user.Id);

            if (employeeTaken)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeNumber),
                    "Another account already uses this employee number."
                );

                ViewBag.EditMode = true;
                return View(user);
            }

            // Unique Email (excluding self)
            var emailUser = await _userManager.FindByEmailAsync(email);

            if (emailUser != null && emailUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Another account already uses this email address."
                );

                ViewBag.EditMode = true;
                return View(user);
            }

            // Apply changes
            user.FirstName = firstName;
            user.LastName = lastName;
            user.HPCSANumber = hpcsa;
            user.EmployeeNumber = employeeNumber;
            user.Email = email;
            user.UserName = email;
            user.PhoneNumb = phone;
            user.Gender = string.IsNullOrWhiteSpace(model.Gender)
                ? user.Gender
                : model.Gender.Trim();
            user.SouthAfricanID = string.IsNullOrWhiteSpace(model.SouthAfricanID)
                ? user.SouthAfricanID
                : model.SouthAfricanID.Trim();

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

        // =========================================================
        // CHANGE PASSWORD
        // GET: /Doctor/ChangePassword
        // =========================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // =========================================================
        // CHANGE PASSWORD
        // POST: /Doctor/ChangePassword
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(currentPassword))
                ModelState.AddModelError(string.Empty, "Current password is required.");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError(string.Empty, "New password is required.");

            if (newPassword != confirmPassword)
                ModelState.AddModelError(
                    string.Empty,
                    "New password and confirmation do not match."
                );

            if (!ModelState.IsValid)
                return View();

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword
            );

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View();
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Password changed successfully.";

            return RedirectToAction(nameof(DoctorProfile));
        }

        // =========================================================
        // LOGOUT
        // POST: /Doctor/Logout
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }
    }
}