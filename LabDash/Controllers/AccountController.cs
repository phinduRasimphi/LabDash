using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace LabDash.Controllers
{
    public class AccountController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<LabUser> _userStore;
        private readonly SignInManager<LabUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notifications;

        public AccountController(
            LabDbContext dbContext,
            UserManager<LabUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<LabUser> userStore,
            IEmailSender emailSender,
            SignInManager<LabUser> signInManager,
            NotificationService notifications)
        {
            _userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailSender = emailSender;
            _signInManager = signInManager;
            _notifications = notifications;
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // ------------------------------------------------
                    // EMAIL CONFIRMATION CHECK
                    // ------------------------------------------------

                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(
                            "",
                            "Please confirm your email address before logging in.");

                        return View(model);
                    }

                    // ------------------------------------------------
                    // PASSWORD LOGIN
                    // ------------------------------------------------

                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] =
                            "Welcome to your work environment!";

                        // ====================================================
                        // ADMIN
                        // ====================================================

                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction(
                                "Index",
                                "Dashboard");
                        }

                        // ====================================================
                        // PATIENT
                        // ====================================================

                        if (await _userManager.IsInRoleAsync(user, "Patient"))
                        {
                            // ------------------------------------------------
                            // WELCOME EMAIL ONLY ONCE
                            // ------------------------------------------------

                            if (!user.WelcomeEmailSent)
                            {
                                string supportEmail =
                                    "LabDashSupport@gmail.com";

                                var emailBody =
                                    BuildWelcomeEmail(
                                        user.FirstName,
                                        supportEmail);

                                try
                                {
                                    // ------------------------------------------------
                                    // TESTING EMAIL
                                    //
                                    // Change to user.Email when ready.
                                    // ------------------------------------------------

                                    string realAccount =
                                        "labdashrsa@gmail.com";

                                    await _emailSender.SendEmailAsync(
                                        realAccount,
                                        $"Welcome to LabDash, {user.FirstName}!",
                                        emailBody);

                                    // Only mark as sent AFTER successful email.
                                    user.WelcomeEmailSent = true;

                                    await _userManager.UpdateAsync(user);
                                }
                                catch
                                {
                                    // Do not prevent login if email fails.
                                    //
                                    // WelcomeEmailSent stays false so that
                                    // another attempt can happen later.
                                }
                            }

                            return RedirectToAction(
                                "Index",
                                "Dashboard");
                        }

                        // ====================================================
                        // LAB TECHNICIAN
                        // ====================================================

                        if (await _userManager.IsInRoleAsync(
                            user,
                            "Lab_Technician"))
                        {
                            return RedirectToAction(
                                "Index",
                                "Dashboard");
                        }

                        // ====================================================
                        // LAB MANAGER
                        // ====================================================

                        if (await _userManager.IsInRoleAsync(
                            user,
                            "Lab_Manager"))
                        {
                            return RedirectToAction(
                                "Index",
                                "Dashboard");
                        }

                        // ====================================================
                        // DOCTOR
                        // ====================================================

                        if (await _userManager.IsInRoleAsync(
                            user,
                            "Doctor"))
                        {
                            return RedirectToAction(
                                "Index",
                                "DoctorHome");
                        }

                        // ====================================================
                        // FALLBACK
                        // ====================================================

                        return RedirectToAction(
                            "Index",
                            "Home");
                    }

                    // ------------------------------------------------
                    // LOGIN FAILED
                    // ------------------------------------------------

                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(
                            "",
                            "Your account is temporarily locked. Please try again later.");
                    }
                    else
                    {
                        ModelState.AddModelError(
                            "",
                            "Invalid password.");
                    }
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        "No account was found with that email address.");
                }
            }

            return View(model);
        }

        // ============================================================
        // WELCOME EMAIL
        // ============================================================

        private static string BuildWelcomeEmail(
            string? firstName,
            string supportEmail)
        {
            var name =
                string.IsNullOrWhiteSpace(firstName)
                    ? "there"
                    : HtmlEncoder.Default.Encode(
                        firstName.Trim());

            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0; padding:0; background-color:#eef3fb; font-family:Segoe UI, Arial, sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0' border='0'
       style='background-color:#eef3fb; padding:24px 0;'>

<tr>
<td align='center'>

<table width='600' cellpadding='0' cellspacing='0' border='0'
       style='max-width:600px; width:100%; background-color:#ffffff; border-radius:16px; overflow:hidden;'>

<tr>
<td align='center'
    bgcolor='#2f6db3'
    style='background-color:#2f6db3; background-image:linear-gradient(135deg,#2f6db3,#18a999); padding:42px 24px;'>

<div style='font-size:46px; line-height:1;'>
    &#129514;
</div>

<h1 style='margin:12px 0 6px 0; color:#ffffff; font-size:30px; font-weight:800;'>
    Hey {name}, welcome to LabDash! &#127881;
</h1>

<p style='margin:0; color:#e6f4ff; font-size:16px;'>
    Your health results, simplified &#8212; all in one place.
</p>

</td>
</tr>

<tr>
<td style='padding:30px 36px 8px 36px; color:#2b3a4a; font-size:16px; line-height:1.6;'>

<p style='margin:0 0 12px 0;'>
    We're so glad you're here, <strong>{name}</strong>! &#128075;
</p>

<p style='margin:0;'>
    LabDash helps you keep track of your laboratory journey
    and stay informed about your health results in one place.
</p>

</td>
</tr>

<tr>
<td style='padding:18px 36px 6px 36px;'>

<table width='100%' cellpadding='0' cellspacing='0' border='0'>

<tr>

<td width='50%' valign='top' style='padding:6px;'>

<div style='background-color:#f1f7ff; border-radius:12px; padding:16px;'>

<div style='font-size:26px;'>
    &#128300;
</div>

<div style='font-weight:700; color:#1f4e8c; margin:6px 0 2px 0;'>
    Track your tests
</div>

<div style='font-size:14px; color:#52606d; line-height:1.5;'>
    Keep up with your laboratory test requests and results.
</div>

</div>

</td>

<td width='50%' valign='top' style='padding:6px;'>

<div style='background-color:#effaf7; border-radius:12px; padding:16px;'>

<div style='font-size:26px;'>
    &#9889;
</div>

<div style='font-weight:700; color:#0f7a6c; margin:6px 0 2px 0;'>
    View your results
</div>

<div style='font-size:14px; color:#52606d; line-height:1.5;'>
    Access your available laboratory results when they are released.
</div>

</div>

</td>

</tr>

<tr>

<td width='50%' valign='top' style='padding:6px;'>

<div style='background-color:#fff7ec; border-radius:12px; padding:16px;'>

<div style='font-size:26px;'>
    &#129657;
</div>

<div style='font-weight:700; color:#b26a00; margin:6px 0 2px 0;'>
    Share with your doctor
</div>

<div style='font-size:14px; color:#52606d; line-height:1.5;'>
    Manage access to your health information when needed.
</div>

</div>

</td>

<td width='50%' valign='top' style='padding:6px;'>

<div style='background-color:#f6f1ff; border-radius:12px; padding:16px;'>

<div style='font-size:26px;'>
    &#128276;
</div>

<div style='font-weight:700; color:#5b3fa3; margin:6px 0 2px 0;'>
    Stay informed
</div>

<div style='font-size:14px; color:#52606d; line-height:1.5;'>
    Receive important notifications about your laboratory journey.
</div>

</div>

</td>

</tr>

</table>

</td>
</tr>

<tr>
<td align='center'
    style='padding:26px 36px 28px 36px; color:#6b7785; font-size:14px; line-height:1.6;'>

You're all set! You can now use LabDash to manage
and view your laboratory information securely.

</td>
</tr>

<tr>

<td align='center'
    style='background-color:#f7f9fc; padding:22px 36px; color:#8a95a3; font-size:13px; line-height:1.6;'>

Questions? We're happy to help at

<a href='mailto:{supportEmail}'
   style='color:#2f6db3; text-decoration:none;'>
    {supportEmail}
</a>

<br/>

Here's to healthier days ahead,

<br/>

<strong style='color:#2b3a4a;'>
    The LabDash Team
</strong>

&#128153;

</td>

</tr>

</table>

</td>
</tr>

</table>

</body>
</html>";
        }

        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null ||
                string.IsNullOrEmpty(user.Email) ||
                !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(
                    user);

            var resetLink =
                Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        token = token,
                        email = user.Email
                    },
                    Request.Scheme);

            if (string.IsNullOrEmpty(resetLink))
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var encodedResetLink =
                HtmlEncoder.Default.Encode(resetLink);

            var emailBody =
                $"<html><head><style>" +
                $"body {{ font-family: Arial, sans-serif; }}" +
                $".cta-button {{ background-color: #2f6db3; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}" +
                $".cta-button:hover {{ background-color: #265580; }}" +
                $".footer {{ margin-top: 20px; font-size: 12px; color: #888; }}" +
                $"</style></head>" +
                $"<body>" +
                $"<h1>Reset your LabDash password</h1>" +
                $"<p>We received a request to reset your password. Click the button below to choose a new one:</p>" +
                $"<p><a class='cta-button' href='{encodedResetLink}'>Reset Password</a></p>" +
                $"<p>If you did not request a password reset, you can safely ignore this email.</p>" +
                $"<div class='footer'><p>LabDash Team</p></div>" +
                $"</body></html>";

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Reset your password",
                    emailBody);
            }
            catch (Exception)
            {
                // Do not expose email service errors to users.
            }

            return RedirectToAction(
                "ForgotPasswordConfirmation",
                "Account");
        }

        // ============================================================
        // FORGOT PASSWORD CONFIRMATION
        // ============================================================

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string token,
            string email)
        {
            if (token == null || email == null)
                return BadRequest();

            var model = new ResetPasswordModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(model);
        }

        // ============================================================
        // RESET PASSWORD CONFIRMATION
        // ============================================================

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // --------------------------------------------------------
            // GET THE CURRENT LOGGED-IN IDENTITY USER
            //
            // IMPORTANT:
            // Do NOT look for a Patient here.
            // Admins, Doctors, Technicians and Managers also
            // have Identity accounts.
            // --------------------------------------------------------

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // --------------------------------------------------------
            // CHANGE PASSWORD
            // --------------------------------------------------------

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);

            if (result.Succeeded)
            {
                // Keep the user signed in after changing password.
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] =
                    "Your password has been changed successfully.";

                // ====================================================
                // ADMIN
                // ====================================================

                if (await _userManager.IsInRoleAsync(
                    user,
                    "Admin"))
                {
                    return RedirectToAction(
                        "AdminProfile",
                        "Admin");
                }

                // ====================================================
                // PATIENT
                // ====================================================

                if (await _userManager.IsInRoleAsync(
                    user,
                    "Patient"))
                {
                    return RedirectToAction(
                        "Profile",
                        "Patient");
                }

                // ====================================================
                // DOCTOR
                // ====================================================

                if (await _userManager.IsInRoleAsync(
                    user,
                    "Doctor"))
                {
                    return RedirectToAction(
                        "Index",
                        "DoctorHome");
                }

                // ====================================================
                // LAB TECHNICIAN
                // ====================================================

                if (await _userManager.IsInRoleAsync(
                    user,
                    "Lab_Technician"))
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard");
                }

                // ====================================================
                // LAB MANAGER
                // ====================================================

                if (await _userManager.IsInRoleAsync(
                    user,
                    "Lab_Manager"))
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard");
                }

                // ====================================================
                // FALLBACK
                // ====================================================

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // --------------------------------------------------------
            // PASSWORD CHANGE FAILED
            // --------------------------------------------------------

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(model);
        }

        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }

        // ============================================================
        // REGISTER - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var model =
                new PatientRegisterViewModel();

            await PopulateMedicalOptions(model);

            return View(model);
        }

        // ============================================================
        // REGISTER - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            PatientRegisterViewModel model)
        {
            // --------------------------------------------------------
            // NORMALISE INPUT
            // --------------------------------------------------------

            model.Email =
                model.Email?.Trim().ToLowerInvariant()
                ?? "";

            model.IDNumber =
                model.IDNumber?.Trim()
                ?? "";

            model.CellphoneNumber =
                model.CellphoneNumber?.Trim()
                ?? "";

            model.Name =
                model.Name?.Trim()
                ?? "";

            model.Surname =
                model.Surname?.Trim()
                ?? "";

            // --------------------------------------------------------
            // GMAIL VALIDATION
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.Email) &&
                !Regex.IsMatch(
                    model.Email,
                    @"^[A-Za-z0-9._%+-]+@gmail\.com$",
                    RegexOptions.IgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Please use a valid Gmail address ending in @gmail.com.");
            }

            // --------------------------------------------------------
            // ID VALIDATION
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.IDNumber) &&
                !Regex.IsMatch(
                    model.IDNumber,
                    @"^\d{13}$"))
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "ID number must contain exactly 13 digits.");
            }

            // --------------------------------------------------------
            // CELLPHONE VALIDATION
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.CellphoneNumber) &&
                !Regex.IsMatch(
                    model.CellphoneNumber,
                    @"^0[6-8][0-9]{8}$"))
            {
                ModelState.AddModelError(
                    nameof(model.CellphoneNumber),
                    "Please enter a valid 10-digit South African cellphone number.");
            }

            // --------------------------------------------------------
            // POSTAL CODE VALIDATION
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.PostalCode) &&
                !Regex.IsMatch(
                    model.PostalCode,
                    @"^\d{4}$"))
            {
                ModelState.AddModelError(
                    nameof(model.PostalCode),
                    "Postal code must contain exactly 4 digits.");
            }

            // --------------------------------------------------------
            // INITIALISE MEDICAL LISTS
            // --------------------------------------------------------

            model.SelectedMedicalConditionIds ??=
                new List<int>();

            model.SelectedAllergyIds ??=
                new List<int>();

            model.SelectedMedicationIds ??=
                new List<int>();

            // --------------------------------------------------------
            // VALIDATE MODEL
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await PopulateMedicalOptions(model);

                return View(model);
            }

            // ========================================================
            // CHECK DUPLICATE EMAIL
            // ========================================================

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this Gmail address already exists.");

                await PopulateMedicalOptions(model);

                return View(model);
            }

            // ========================================================
            // CHECK DUPLICATE ID NUMBER
            // ========================================================

            var existingPatient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.IDNumber == model.IDNumber);

            if (existingPatient != null)
            {
                ModelState.AddModelError(
                    nameof(model.IDNumber),
                    "A patient account with this ID number already exists.");

                await PopulateMedicalOptions(model);

                return View(model);
            }

            // ========================================================
            // CREATE IDENTITY USER
            // ========================================================

            var user = new LabUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name,
                LastName = model.Surname,
                PhoneNumb = model.CellphoneNumber
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await PopulateMedicalOptions(model);

                return View(model);
            }

            // ========================================================
            // ASSIGN PATIENT ROLE
            // ========================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Patient");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await PopulateMedicalOptions(model);

                return View(model);
            }

            // ========================================================
            // CUSTOM MEDICAL INFORMATION
            // ========================================================

            string customCondition =
                string.IsNullOrWhiteSpace(
                    model.OtherMedicalCondition)
                    ? "None"
                    : model.OtherMedicalCondition.Trim();

            string customAllergy =
                string.IsNullOrWhiteSpace(
                    model.OtherAllergy)
                    ? "None"
                    : model.OtherAllergy.Trim();

            string customMedication =
                string.IsNullOrWhiteSpace(
                    model.OtherMedication)
                    ? "None"
                    : model.OtherMedication.Trim();

            // ========================================================
            // CREATE PATIENT
            // ========================================================

            var newPatient = new Patient
            {
                UserId = user.Id,

                Name = model.Name,
                Surname = model.Surname,
                IDNumber = model.IDNumber,
                DOB = model.DOB,
                CellphoneNumber = model.CellphoneNumber,
                Email = model.Email,

                // ----------------------------------------------------
                // ADDRESS
                // ----------------------------------------------------

                HomeAddress = model.AddressLine1,

                AddressLine1 =
                    model.AddressLine1.Trim(),

                AddressLine2 =
                    string.IsNullOrWhiteSpace(model.AddressLine2)
                        ? null
                        : model.AddressLine2.Trim(),

                Suburb =
                    model.Suburb.Trim(),

                City =
                    model.City.Trim(),

                Province =
                    model.Province.Trim(),

                PostalCode =
                    string.IsNullOrWhiteSpace(model.PostalCode)
                        ? null
                        : model.PostalCode.Trim(),

                // ----------------------------------------------------
                // CUSTOM MEDICAL INFORMATION
                // ----------------------------------------------------

                MedicalConditions =
                    customCondition,

                Allergies =
                    customAllergy,

                Medication =
                    customMedication
            };

            _context.Patients.Add(newPatient);

            await _context.SaveChangesAsync();

            // ========================================================
            // EXISTING MEDICAL CONDITIONS
            // ========================================================

            foreach (
                var conditionId
                in model.SelectedMedicalConditionIds.Distinct())
            {
                var conditionExists =
                    await _context.MedicalConditions
                        .AnyAsync(x =>
                            x.MedicalConditionId == conditionId &&
                            x.IsActive);

                if (conditionExists)
                {
                    _context.PatientMedicalConditions.Add(
                        new PatientMedicalCondition
                        {
                            PatientID =
                                newPatient.PatientID,

                            MedicalConditionId =
                                conditionId,

                            DiagnosisDate =
                                DateTime.Now
                        });
                }
            }

            // ========================================================
            // EXISTING ALLERGIES
            // ========================================================

            foreach (
                var allergyId
                in model.SelectedAllergyIds.Distinct())
            {
                var allergyExists =
                    await _context.Allergies
                        .AnyAsync(x =>
                            x.AllergyId == allergyId &&
                            x.IsActive);

                if (allergyExists)
                {
                    _context.PatientAllergies.Add(
                        new PatientAllergy
                        {
                            PatientID =
                                newPatient.PatientID,

                            AllergyId =
                                allergyId,

                            RecordedDate =
                                DateTime.Now
                        });
                }
            }

            // ========================================================
            // EXISTING MEDICATIONS
            // ========================================================

            foreach (
                var medicationId
                in model.SelectedMedicationIds.Distinct())
            {
                var medicationExists =
                    await _context.Medications
                        .AnyAsync(x =>
                            x.MedicationId == medicationId &&
                            x.IsActive);

                if (medicationExists)
                {
                    _context.PatientMedications.Add(
                        new PatientMedication
                        {
                            PatientID =
                                newPatient.PatientID,

                            MedicationId =
                                medicationId,

                            StartDate =
                                DateTime.Now
                        });
                }
            }

            await _context.SaveChangesAsync();

            // ========================================================
            // NOTIFY DOCTORS
            // ========================================================

            await _notifications.SendToRoleAsync(
                roleName: "Doctor",

                title: "New patient registered",

                message:
                    $"{newPatient.Name} {newPatient.Surname} has created a patient profile.",

                type: "NewPatient",

                linkUrl:
                    Url.Action(
                        "SharedWithMe",
                        "Doctor"),

                relatedPatientId:
                    newPatient.PatientID,

                actorUserId:
                    user.Id
            );

            // ========================================================
            // EMAIL CONFIRMATION
            // ========================================================

            var code =
                await _userManager
                    .GenerateEmailConfirmationTokenAsync(
                        user);

            var callbackUrl =
                Url.Action(
                    "EmailVerified",
                    "Account",
                    new
                    {
                        userId = user.Id,
                        code = Uri.EscapeDataString(code)
                    },
                    Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(
                    model.Email,
                    "Confirm your LabDash email",
                    $"<p>Welcome to LabDash!</p>" +
                    $"<p>Please confirm your email address by " +
                    $"<a href='{callbackUrl}'>clicking here</a>.</p>");
            }
            catch
            {
                // Account has already been created.
                // Email confirmation can be handled separately.
            }

            return RedirectToAction(
                "RegisterConfirmation");
        }

        // ============================================================
        // POPULATE MEDICAL OPTIONS
        // ============================================================

        private async Task PopulateMedicalOptions(
            PatientRegisterViewModel model)
        {
            // --------------------------------------------------------
            // MEDICAL CONDITIONS
            // --------------------------------------------------------

            model.MedicalConditionOptions =
                await _context.MedicalConditions
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ConditionName)
                    .Select(x => new SelectListItem
                    {
                        Value =
                            x.MedicalConditionId.ToString(),

                        Text =
                            x.ConditionName,

                        Selected =
                            model.SelectedMedicalConditionIds
                                .Contains(x.MedicalConditionId)
                    })
                    .ToListAsync();

            // --------------------------------------------------------
            // ALLERGIES
            // --------------------------------------------------------

            model.AllergyOptions =
                await _context.Allergies
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.AllergyName)
                    .Select(x => new SelectListItem
                    {
                        Value =
                            x.AllergyId.ToString(),

                        Text =
                            x.AllergyName,

                        Selected =
                            model.SelectedAllergyIds
                                .Contains(x.AllergyId)
                    })
                    .ToListAsync();

            // --------------------------------------------------------
            // MEDICATIONS
            // --------------------------------------------------------

            model.MedicationOptions =
                await _context.Medications
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.MedicationName)
                    .Select(x => new SelectListItem
                    {
                        Value =
                            x.MedicationId.ToString(),

                        Text =
                            x.MedicationName,

                        Selected =
                            model.SelectedMedicationIds
                                .Contains(x.MedicationId)
                    })
                    .ToListAsync();
        }

        // ============================================================
        // REGISTER CONFIRMATION
        // ============================================================

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // ============================================================
        // EMAIL VERIFIED
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EmailVerified(
            string userId,
            string code)
        {
            var user =
                await _userManager.FindByIdAsync(
                    userId);

            if (user == null)
                return NotFound();

            var result =
                await _userManager.ConfirmEmailAsync(
                    user,
                    Uri.UnescapeDataString(code));

            return result.Succeeded
                ? View("EmailConfirmed")
                : View("Error");
        }
    }
}