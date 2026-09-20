using LabDash.Areas.Identity.Data;
using LabDash.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<LabUser> _userManager;

        public ProfileController(
            UserManager<LabUser> userManager)
        {
            _userManager = userManager;
        }


        // ============================================================
        // PROFILE PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                UserName = user.UserName,

                FirstName = user.FirstName,

                LastName = user.LastName,

                Email = user.Email ?? "",

                PhoneNumber = user.PhoneNumb,

                Gender = user.Gender,

                AccountCreated = user.Timestamp_AccountCreated
            };

            return View(model);
        }


        // ============================================================
        // UPDATE PROFILE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            ProfileViewModel model)
        {
            // We do NOT validate password fields
            ModelState.Remove(nameof(model.CurrentPassword));
            ModelState.Remove(nameof(model.NewPassword));
            ModelState.Remove(nameof(model.ConfirmPassword));

            if (!ModelState.IsValid)
            {
                // Reload values that are not editable
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return NotFound();
                }

                model.UserName = currentUser.UserName;

                model.AccountCreated =
                    currentUser.Timestamp_AccountCreated;

                return View("Index", model);
            }


            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }


            // ========================================================
            // UPDATE USER INFORMATION
            // ========================================================

            user.FirstName = model.FirstName.Trim();

            user.LastName = model.LastName.Trim();

            user.PhoneNumb = model.PhoneNumber?.Trim();

            user.Gender = model.Gender;


            // ========================================================
            // UPDATE EMAIL
            // ========================================================

            if (user.Email != model.Email)
            {
                var emailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        model.Email.Trim());

                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError(
                            "Email",
                            error.Description);
                    }

                    model.UserName = user.UserName;

                    model.AccountCreated =
                        user.Timestamp_AccountCreated;

                    return View("Index", model);
                }

                // Keep Identity username/email in sync
                user.UserName = model.Email.Trim();
            }


            // ========================================================
            // SAVE CHANGES
            // ========================================================

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                model.UserName = user.UserName;

                model.AccountCreated =
                    user.Timestamp_AccountCreated;

                return View("Index", model);
            }


            TempData["Success"] =
                "Your profile information was successfully updated.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ProfileViewModel model)
        {
            // Only validate password fields
            ModelState.Remove(nameof(model.FirstName));
            ModelState.Remove(nameof(model.LastName));
            ModelState.Remove(nameof(model.Email));
            ModelState.Remove(nameof(model.PhoneNumber));
            ModelState.Remove(nameof(model.Gender));

            if (!ModelState.IsValid)
            {
                await ReloadProfileInformation(model);

                return View("Index", model);
            }


            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }


            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Please enter your current password.");

                await ReloadProfileInformation(model);

                return View("Index", model);
            }


            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Please enter a new password.");

                await ReloadProfileInformation(model);

                return View("Index", model);
            }


            // ========================================================
            // CHANGE PASSWORD USING IDENTITY
            // ========================================================

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "NewPassword",
                        error.Description);
                }

                await ReloadProfileInformation(model);

                return View("Index", model);
            }


            // ========================================================
            // PASSWORD SUCCESS
            // ========================================================

            TempData["Success"] =
                "Your password has been changed successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // RELOAD PROFILE INFORMATION
        // ============================================================

        private async Task ReloadProfileInformation(
            ProfileViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return;
            }

            model.UserName = user.UserName;

            model.FirstName = user.FirstName;

            model.LastName = user.LastName;

            model.Email = user.Email ?? "";

            model.PhoneNumber = user.PhoneNumb;

            model.Gender = user.Gender;

            model.AccountCreated =
                user.Timestamp_AccountCreated;
        }
    }
}