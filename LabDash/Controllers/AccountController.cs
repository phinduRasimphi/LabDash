using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    public class AccountController : Controller
    {

        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<LabUser> _userStore;
        private readonly SignInManager<LabUser> _signInManager;

        public AccountController(LabDbContext dbContext,
            UserManager<LabUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<LabUser> userStore,

            SignInManager<LabUser> signInManager)

        {
            _userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _userStore = userStore;

            _signInManager = signInManager;

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
                            return RedirectToAction("Dashboard", "Admin");

                        if (await _userManager.IsInRoleAsync(user, "Doctor"))
                            return RedirectToAction("Dashboard", "Doctor");

                        if (await _userManager.IsInRoleAsync(user, "Lab_Technician"))
                            return RedirectToAction("Dashboard", "LabTechnician");

                        if (await _userManager.IsInRoleAsync(user, "Lab_Manager"))
                            return RedirectToAction("Dashboard", "LabManager");

                        if (await _userManager.IsInRoleAsync(user, "Patient"))
                            return RedirectToAction("Dashboard", "Patient");

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
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}