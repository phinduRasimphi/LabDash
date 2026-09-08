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
        private readonly IEmailSender _emailSender;

        public DoctorController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // =========================================================
        // MANAGE PATIENTS
        // GET: /Doctor/ManagePatients
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

                string cleanID =
                    searchIDNumber.Trim();

                var patient =
                    await _context.Patients
                        .FirstOrDefaultAsync(
                            p => p.IDNumber == cleanID
                        );

                if (patient != null)
                {
                    vm.SearchResult =
                        new PatientDetailsViewModel
                        {
                            PatientID = patient.PatientID,

                            UserId = patient.UserId,

                            Name = patient.Name,

                            Surname = patient.Surname,

                            IDNumber = patient.IDNumber,

                            CellphoneNumber =
                                patient.CellphoneNumber,

                            DOB = patient.DOB,

                            Email = patient.Email,

                            HomeAddress =
                                patient.HomeAddress,

                            MedicalConditions =
                                patient.MedicalConditions,

                            Allergies =
                                patient.Allergies,

                            Medication =
                                patient.Medication
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
        public async Task<IActionResult> MyPatients()
        {
            var patients =
                await _context.Patients
                    .OrderBy(p => p.Surname)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

            return View(patients);
        }

        // =========================================================
        // CREATE PATIENT - GET
        // GET: /Doctor/CreatePatient
        // =========================================================

        [HttpGet]
        public IActionResult CreatePatient(
            string? idNumber)
        {
            var vm =
                new PatientCreateViewModel();

            if (!string.IsNullOrWhiteSpace(idNumber))
            {
                vm.IDNumber =
                    idNumber.Trim();
            }

            return View(vm);
        }

        // =========================================================
        // CREATE PATIENT - POST
        // POST: /Doctor/CreatePatient
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(
            PatientCreateViewModel model)
        {
            // =====================================================
            // CHECK MODEL
            // =====================================================

            if (model == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Patient information could not be received."
                );

                return View(
                    new PatientCreateViewModel()
                );
            }

            // =====================================================
            // VALIDATE REQUIRED FIELDS
            // =====================================================

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Patient name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.Surname))
            {
                ModelState.AddModelError(
                    nameof(model.Surname),
                    "Patient surname is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.IDNumber))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "South African ID number is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.CellphoneNumber))
            {
                ModelState.AddModelError(
                    nameof(model.CellphoneNumber),
                    "Cellphone number is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.HomeAddress))
            {
                ModelState.AddModelError(
                    nameof(model.HomeAddress),
                    "Home address is required."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // =====================================================
            // CLEAN INPUT
            // =====================================================

            string name =
                model.Name?.Trim() ?? string.Empty;

            string surname =
                model.Surname?.Trim() ?? string.Empty;

            string idNumber =
                model.IDNumber?.Trim() ?? string.Empty;

            string cellphone =
                model.CellphoneNumber?.Trim() ?? string.Empty;

            string email =
                model.Email?.Trim().ToLower() ?? string.Empty;

            string homeAddress =
                model.HomeAddress?.Trim() ?? string.Empty;

            string medicalConditions =
                string.IsNullOrWhiteSpace(
                    model.MedicalConditions)
                    ? "None"
                    : model.MedicalConditions.Trim();

            string allergies =
                string.IsNullOrWhiteSpace(
                    model.Allergies)
                    ? "None"
                    : model.Allergies.Trim();

            string medication =
                string.IsNullOrWhiteSpace(
                    model.Medication)
                    ? "None"
                    : model.Medication.Trim();

            // =====================================================
            // CHECK DUPLICATE PATIENT ID
            // =====================================================

            bool idExists =
                await _context.Patients
                    .AnyAsync(
                        p => p.IDNumber == idNumber
                    );

            if (idExists)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "A patient with this South African ID number already exists."
                );

                return View(model);
            }

            // =====================================================
            // CHECK DUPLICATE EMAIL
            // =====================================================

            var existingUser =
                await _userManager.FindByEmailAsync(
                    email
                );

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email address already exists."
                );

                return View(model);
            }

            // =====================================================
            // CHECK DUPLICATE SOUTH AFRICAN ID IN IDENTITY
            // =====================================================

            var existingIdUser =
                await _userManager.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.SouthAfricanID ==
                            idNumber
                    );

            if (existingIdUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "An account with this South African ID number already exists."
                );

                return View(model);
            }

            // =====================================================
            // GENERATE PASSWORD
            // =====================================================

            string generatedPassword =
                GenerateTemporaryPassword();

            // =====================================================
            // GENERATE UNIQUE EMPLOYEE NUMBER
            // =====================================================

            string patientEmployeeNumber =
                await GeneratePatientEmployeeNumberAsync();

            // =====================================================
            // CREATE IDENTITY USER
            // =====================================================

            var user =
                new LabUser
                {
                    UserName = email,

                    Email = email,

                    FirstName = name,

                    LastName = surname,

                    Gender = "Not Specified",

                    PhoneNumb = cellphone,

                    SouthAfricanID = idNumber,

                    EmployeeNumber =
                        patientEmployeeNumber,

                    HPCSANumber =
                        "N/A",

                    MustChangePassword =
                        true
                };

            // =====================================================
            // CREATE IDENTITY ACCOUNT
            // =====================================================

            IdentityResult createResult;

            try
            {
                createResult =
                    await _userManager.CreateAsync(
                        user,
                        generatedPassword
                    );
            }
            catch (Exception ex)
            {
                string error =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient account could not be created. " +
                    error
                );

                return View(model);
            }

            // =====================================================
            // IDENTITY CREATION FAILED
            // =====================================================

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }

            // =====================================================
            // ASSIGN PATIENT ROLE
            // =====================================================

            IdentityResult roleResult;

            try
            {
                roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "Patient"
                    );
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                string error =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The Patient role could not be assigned. " +
                    error
                );

                return View(model);
            }

            // =====================================================
            // ROLE ASSIGNMENT FAILED
            // =====================================================

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }

            // =====================================================
            // CREATE PATIENT
            // =====================================================

            var patient =
                new Patient
                {
                    UserId = user.Id,

                    Name = name,

                    Surname = surname,

                    IDNumber = idNumber,

                    CellphoneNumber = cellphone,

                    DOB = model.DOB,

                    Email = email,

                    HomeAddress = homeAddress,

                    MedicalConditions =
                        medicalConditions,

                    Allergies =
                        allergies,

                    Medication =
                        medication
                };

            _context.Patients.Add(patient);

            // =====================================================
            // SAVE PATIENT
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                await _userManager.DeleteAsync(user);

                string error =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient account could not be created. " +
                    error
                );

                return View(model);
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);

                string error =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while saving the patient. " +
                    error
                );

                return View(model);
            }

            // =====================================================
            // SEND LOGIN DETAILS
            // =====================================================

            bool emailSent = false;

            try
            {
                string? loginUrl =
                    Url.Action(
                        "Login",
                        "Account",
                        null,
                        Request.Scheme
                    );

                string emailBody =
                    $@"
                    <html>
                    <body style='font-family:Arial,sans-serif;'>

                        <h2 style='color:#126b65;'>
                            Welcome to NMB LAB
                        </h2>

                        <p>
                            Hi {name},
                        </p>

                        <p>
                            Your patient account has been
                            successfully created.
                        </p>

                        <p>
                            Your login details are:
                        </p>

                        <table
                            cellpadding='8'
                            cellspacing='0'
                            style='border-collapse:collapse;'>

                            <tr>
                                <td>
                                    <strong>Username:</strong>
                                </td>

                                <td>
                                    {email}
                                </td>
                            </tr>

                            <tr>
                                <td>
                                    <strong>Temporary Password:</strong>
                                </td>

                                <td>
                                    {generatedPassword}
                                </td>
                            </tr>

                        </table>

                        <p>
                            You will be required to change
                            your password when you first log in.
                        </p>

                        <p>
                            <a
                                href='{loginUrl}'
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
                            <strong>
                                NMB Haematology Laboratory
                            </strong>
                        </p>

                    </body>
                    </html>
                    ";

                await _emailSender.SendEmailAsync(
                    email,
                    "Your NMB LAB Patient Account",
                    emailBody
                );

                emailSent = true;
            }
            catch
            {
                // Patient remains registered
                // even if email fails.
            }

            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

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

            return RedirectToAction(
                nameof(ManagePatients)
            );
        }

        // =========================================================
        // EDIT PATIENT
        // GET: /Doctor/EditPatient/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditPatient(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.PatientID == id
                    );

            if (patient == null)
            {
                return NotFound();
            }

            var vm =
                new PatientCreateViewModel
                {
                    Name =
                        patient.Name,

                    Surname =
                        patient.Surname,

                    IDNumber =
                        patient.IDNumber,

                    DOB =
                        patient.DOB,

                    CellphoneNumber =
                        patient.CellphoneNumber,

                    Email =
                        patient.Email,

                    HomeAddress =
                        patient.HomeAddress,

                    MedicalConditions =
                        patient.MedicalConditions,

                    Allergies =
                        patient.Allergies,

                    Medication =
                        patient.Medication
                };

            ViewBag.PatientID =
                patient.PatientID;

            return View(vm);
        }

        // =========================================================
        // UPDATE PATIENT
        // POST: /Doctor/UpdatePatient
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatient(
            PatientDetailsViewModel model)
        {
            // =====================================================
            // CHECK MODEL
            // =====================================================

            if (model == null)
            {
                TempData["Error"] =
                    "The patient information could not be received.";

                return RedirectToAction(
                    nameof(ManagePatients)
                );
            }

            // =====================================================
            // REQUIRED FIELD VALIDATION
            // =====================================================

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Patient name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.Surname))
            {
                ModelState.AddModelError(
                    nameof(model.Surname),
                    "Patient surname is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.IDNumber))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "South African ID number is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.CellphoneNumber))
            {
                ModelState.AddModelError(
                    nameof(model.CellphoneNumber),
                    "Cellphone number is required."
                );
            }

            if (string.IsNullOrWhiteSpace(model.HomeAddress))
            {
                ModelState.AddModelError(
                    nameof(model.HomeAddress),
                    "Home address is required."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "EditPatient",
                    model
                );
            }

            // =====================================================
            // CLEAN VALUES SAFELY
            // =====================================================

            string name =
                model.Name?.Trim() ??
                string.Empty;

            string surname =
                model.Surname?.Trim() ??
                string.Empty;

            string idNumber =
                model.IDNumber?.Trim() ??
                string.Empty;

            string cellphone =
                model.CellphoneNumber?.Trim() ??
                string.Empty;

            string email =
                model.Email?.Trim().ToLower() ??
                string.Empty;

            string homeAddress =
                model.HomeAddress?.Trim() ??
                string.Empty;

            string medicalConditions =
                string.IsNullOrWhiteSpace(
                    model.MedicalConditions)
                    ? "None"
                    : model.MedicalConditions.Trim();

            string allergies =
                string.IsNullOrWhiteSpace(
                    model.Allergies)
                    ? "None"
                    : model.Allergies.Trim();

            string medication =
                string.IsNullOrWhiteSpace(
                    model.Medication)
                    ? "None"
                    : model.Medication.Trim();

            // =====================================================
            // FIND PATIENT
            // =====================================================

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p =>
                            p.PatientID ==
                            model.PatientID
                    );

            if (patient == null)
            {
                TempData["Error"] =
                    "The patient record could not be found.";

                return RedirectToAction(
                    nameof(ManagePatients)
                );
            }

            // =====================================================
            // CHECK DUPLICATE PATIENT ID
            // =====================================================

            bool duplicatePatientID =
                await _context.Patients
                    .AnyAsync(
                        p =>
                            p.IDNumber == idNumber &&
                            p.PatientID !=
                            model.PatientID
                    );

            if (duplicatePatientID)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "Another patient already uses this South African ID number."
                );

                return View(
                    "EditPatient",
                    model
                );
            }

            // =====================================================
            // FIND IDENTITY USER
            // =====================================================

            LabUser? user = null;

            if (!string.IsNullOrWhiteSpace(
                patient.UserId))
            {
                user =
                    await _userManager.FindByIdAsync(
                        patient.UserId
                    );
            }

            // =====================================================
            // CHECK IDENTITY EMAIL
            // =====================================================

            if (user != null)
            {
                var existingEmailUser =
                    await _userManager
                        .FindByEmailAsync(email);

                if (
                    existingEmailUser != null &&
                    existingEmailUser.Id != user.Id
                )
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Another account already uses this email address."
                    );

                    return View(
                        "EditPatient",
                        model
                    );
                }

                // =================================================
                // CHECK IDENTITY SA ID
                // =================================================

                var existingIdUser =
                    await _userManager.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.SouthAfricanID ==
                                idNumber &&
                                u.Id != user.Id
                        );

                if (existingIdUser != null)
                {
                    ModelState.AddModelError(
                        nameof(model.IDNumber),
                        "Another account already uses this South African ID number."
                    );

                    return View(
                        "EditPatient",
                        model
                    );
                }
            }

            // =====================================================
            // UPDATE PATIENT
            // =====================================================

            patient.Name =
                name;

            patient.Surname =
                surname;

            patient.IDNumber =
                idNumber;

            patient.DOB =
                model.DOB;

            patient.CellphoneNumber =
                cellphone;

            patient.Email =
                email;

            patient.HomeAddress =
                homeAddress;

            patient.MedicalConditions =
                medicalConditions;

            patient.Allergies =
                allergies;

            patient.Medication =
                medication;

            // =====================================================
            // UPDATE IDENTITY USER
            // =====================================================

            if (user != null)
            {
                user.FirstName =
                    name;

                user.LastName =
                    surname;

                user.PhoneNumb =
                    cellphone;

                user.SouthAfricanID =
                    idNumber;

                user.Email =
                    email;

                user.UserName =
                    email;

                var updateUserResult =
                    await _userManager
                        .UpdateAsync(user);

                if (!updateUserResult.Succeeded)
                {
                    foreach (
                        var error
                        in updateUserResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    return View(
                        "EditPatient",
                        model
                    );
                }
            }

            // =====================================================
            // SAVE DATABASE CHANGES
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                string databaseError =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "The patient information could not be saved. " +
                    databaseError
                );

                return View(
                    "EditPatient",
                    model
                );
            }
            catch (Exception ex)
            {
                string error =
                    ex.InnerException?.Message
                    ?? ex.Message;

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while saving the patient. " +
                    error
                );

                return View(
                    "EditPatient",
                    model
                );
            }

            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "Patient record updated successfully.";

            return RedirectToAction(
                nameof(ManagePatients)
            );
        }

        // =========================================================
        // GENERATE UNIQUE PATIENT EMPLOYEE NUMBER
        // =========================================================

        private async Task<string>
            GeneratePatientEmployeeNumberAsync()
        {
            string employeeNumber;

            do
            {
                employeeNumber =
                    "PAT-" +
                    RandomNumberGenerator
                        .GetInt32(
                            100000,
                            999999
                        )
                        .ToString();
            }
            while (
                await _userManager.Users
                    .AnyAsync(
                        u =>
                            u.EmployeeNumber ==
                            employeeNumber
                    )
            );

            return employeeNumber;
        }

        // =========================================================
        // GENERATE SECURE TEMPORARY PASSWORD
        // =========================================================

        private string GenerateTemporaryPassword()
        {
            const string uppercase =
                "ABCDEFGHJKLMNPQRSTUVWXYZ";

            const string lowercase =
                "abcdefghijkmnopqrstuvwxyz";

            const string numbers =
                "23456789";

            const string special =
                "!@#$%";

            // -----------------------------------------------------
            // GUARANTEE REQUIRED CHARACTER TYPES
            // -----------------------------------------------------

            var password =
                new List<char>
                {
                    GetRandomCharacter(uppercase),

                    GetRandomCharacter(lowercase),

                    GetRandomCharacter(numbers),

                    GetRandomCharacter(special)
                };

            // -----------------------------------------------------
            // ALL CHARACTERS
            // -----------------------------------------------------

            const string allCharacters =
                uppercase +
                lowercase +
                numbers +
                special;

            // -----------------------------------------------------
            // GENERATE REMAINING CHARACTERS
            // -----------------------------------------------------

            while (password.Count < 12)
            {
                password.Add(
                    GetRandomCharacter(
                        allCharacters
                    )
                );
            }

            // -----------------------------------------------------
            // SECURE SHUFFLE
            // -----------------------------------------------------

            for (
                int i = password.Count - 1;
                i > 0;
                i--)
            {
                int j =
                    RandomNumberGenerator
                        .GetInt32(
                            i + 1
                        );

                (
                    password[i],
                    password[j]
                ) =
                (
                    password[j],
                    password[i]
                );
            }

            return new string(
                password.ToArray()
            );
        }

        // =========================================================
        // GET RANDOM CHARACTER
        // =========================================================

        private char GetRandomCharacter(
            string characters)
        {
            if (string.IsNullOrEmpty(characters))
            {
                throw new ArgumentException(
                    "Character set cannot be empty.",
                    nameof(characters)
                );
            }

            int index =
                RandomNumberGenerator
                    .GetInt32(
                        characters.Length
                    );

            return characters[index];
        }
    }
}