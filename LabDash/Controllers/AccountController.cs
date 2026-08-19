using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

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

        public AccountController(LabDbContext dbContext,
            UserManager<LabUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<LabUser> userStore,

          IEmailSender emailSender,



            SignInManager<LabUser> signInManager)

        {
            _userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _userStore = userStore;

            _signInManager = signInManager;
            _emailSender = emailSender;
        }


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
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError("", "Email not confirmed.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Welcome to your work environment!";

                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            string applicationName = "LabDash";

                            string supportEmail = "LabDashSupport@gmail.com";
                            await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                           $"<html> <head> <style> body {{ font-family: Arial, sans-serif; }} " +

                           $" padding: 10px 20px;" +
                           $" text-decoration: none; border-radius: 5px; }}" +
                           $".cta-button:hover {{ background-color: #265580; }}" +
                           $".footer {{ margin-top: 20px; font-size: 12px; color: #888; }}" +
       $"  </style>" +
       $"</head>" +
       $"<body>" +
       $"" +
       $"<h1>Welcome to {applicationName}!</h1>" +
       $"<p>Dear User</p>" +
       $"<p>Thank you for registering with {applicationName}! We're excited to have you on board as our friend. Before you can start using your daily activites, please confirm your email address by clicking the button below:</p>" +
       $"<p><a class='cta-button' href=LabDashSupport>Confirm Email Address</a></p>" +
       $"  <p>If you did not register for an account with {applicationName}, please ignore this email. It's possible that someone entered your email address by mistake.</p>" +
       $"<p>If you have any questions or need assistance, please don't hesitate to contact our support team at {supportEmail}.</p>" +
       $"<div class='footer'>" +
       $" <p>Thank you for logging to ,</p>" +
       $" <p>{applicationName} Team</p>" +
       $"</div>" +
       $" </body>" +
       $"</html>");
                            return RedirectToAction("Index", "Dashboard");

                        }

                        if (await _userManager.IsInRoleAsync(user, "Patient"))
                        {
                            string applicationName = "LabDash";

                            string supportEmail = "LabDashSupport@gmail.com";
                            string RealAccount = "labdashrsa@gmail.com";

                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);


                            var callbackUrl = $"https://3f60-41-145-194-166.ngrok-free.app/Registerdefowo/EmailVerified?userId={user.Id}&code={Uri.EscapeDataString(code)}";
                            await _emailSender.SendEmailAsync(RealAccount, "Confirm your email",
                           $"<html> <head> <style> body {{ font-family: Arial, sans-serif; }} " +

                           $" padding: 10px 20px;" +
                           $" text-decoration: none; border-radius: 5px; }}" +
                           $".cta-button:hover {{ background-color: #265580; }}" +
                           $".footer {{ margin-top: 20px; font-size: 12px; color: #888; }}" +
       $"  </style>" +
       $"</head>" +
       $"<body>" +
       $"" +
       $"<h1>Welcome to {applicationName}!</h1>" +
       $"<p>Dear User</p>" +
       $"<p>Thank you for registering with {applicationName}! We're excited to have you on board as our friend. Before you can start using your daily activites, please confirm your email address by clicking the button below:</p>" +
       $"<p><a class='cta-button' href=LabDashSupport>Confirm Email Address</a></p>" +
       $"  <p>If you did not register for an account with {applicationName}, please ignore this email. It's possible that someone entered your email address by mistake.</p>" +
       $"<p>If you have any questions or need assistance, please don't hesitate to contact our support team at {supportEmail}.</p>" +
       $"<div class='footer'>" +
       $" <p>Thank you for logging to ,</p>" +
       $" <p>{applicationName} Team</p>" +
       $"</div>" +
       $" </body>" +
       $"</html>");

                            return RedirectToAction("Index", "Dashboard");


                        }



                        if (await _userManager.IsInRoleAsync(user, "Lab_Technician"))
                            return RedirectToAction("Index", "Dashboard");

                        if (await _userManager.IsInRoleAsync(user, "Lab_Manager"))
                            return RedirectToAction("Index", "Dashboard");

                        if (await _userManager.IsInRoleAsync(user, "Doctor"))
                            return RedirectToAction("Index", "Dashboard");

                        return RedirectToAction("Index", "Home");
                    }

                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError("", "Account is locked.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid password.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User not found.");
                }
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || string.IsNullOrEmpty(user.Email) || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token, email = user.Email },
                Request.Scheme);

            if (string.IsNullOrEmpty(resetLink))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var encodedResetLink = HtmlEncoder.Default.Encode(resetLink);

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
                await _emailSender.SendEmailAsync(user.Email, "Reset your password", emailBody);
            }
            catch (Exception ex)
            {
                //  _logger?.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return RedirectToAction("ForgotPasswordConfirmation", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
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
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.Password);

            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View(new PatientRegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new LabUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name,
                LastName = model.Surname,
                PhoneNumb = model.CellphoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Patient");

            _context.Patients.Add(new Patient
            {
                UserId = user.Id,
                Name = model.Name,
                Surname = model.Surname,
                IDNumber = model.IDNumber,
                DOB = model.DOB,
                CellphoneNumber = model.CellphoneNumber,
                HomeAddress = model.HomeAddress,
                Email = model.Email
            });
            await _context.SaveChangesAsync();

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("EmailVerified", "Account",
                new { userId = user.Id, code = Uri.EscapeDataString(code) }, Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                $"<p>Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.</p>");

            return RedirectToAction("RegisterConfirmation");
        }

        [HttpGet]
        public IActionResult RegisterConfirmation() => View();

        [HttpGet]
        public async Task<IActionResult> EmailVerified(string userId, string code)

        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var result = await _userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(code));
            return result.Succeeded ? View("EmailConfirmed") : View("Error");
        }

        

    }
}
