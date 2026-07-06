using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

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

                        if (await _userManager.IsInRoleAsync(user, "Doctor"))
                        {
                            string applicationName = "LabDash";

                            string supportEmail = "LabDashSupport@gmail.com";
                            string RealAccount = "phindu.ras2003@gmail.com";

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

                        if (await _userManager.IsInRoleAsync(user, "Patient"))
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

            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new
                {
                    token = token,
                    email = user.Email
                },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset Password",
                $"Click <a href='{resetLink}'>here</a> to reset your password.");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
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


        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}