using LabDash.Areas.Identity.Data;
using LabDash.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class ProfileController : Controller
    {
        private readonly UserManager<LabUser> _userManager;
        private readonly SignInManager<LabUser> _signInManager;

        public ProfileController(
            UserManager<LabUser> userManager,
            SignInManager<LabUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        //-----------------------------------------------------
        // Profile
        //-----------------------------------------------------

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var model = new ProfileViewModel
            {
                FullName = user.FirstName + " " + user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumb,
                Gender = user.Gender,
                AccountCreated = user.Timestamp_AccountCreated
            };

            return View(model);
        }

        //-----------------------------------------------------
        // Change Password
        //-----------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                TempData["Success"] =
                    "Password changed successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.FullName = user.FirstName + " " + user.LastName;
            model.Email = user.Email;
            model.UserName = user.UserName;
            model.PhoneNumber = user.PhoneNumb;
            model.Gender = user.Gender;
            model.AccountCreated = user.Timestamp_AccountCreated;

            return View("Index", model);
        }
    }
}
