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

            var model = new ProfileViewModel();

            FillEditableFields(model, user);
            FillLockedFields(model, user);

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
            // Password fields belong to the other form
            ModelState.Remove(nameof(model.CurrentPassword));
            ModelState.Remove(nameof(model.NewPassword));
            ModelState.Remove(nameof(model.ConfirmPassword));

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                // Locked fields always come from the database, never the form
                FillLockedFields(model, user);

                return View("Index", model);
            }


            // ========================================================
            // UPDATE EDITABLE INFORMATION ONLY
            // Gender, EmployeeNumber, IDNumber and AccountCreated are
            // locked and are intentionally NOT copied from the form.
            // ========================================================

            user.FirstName = model.FirstName.Trim();

            user.LastName = model.LastName.Trim();

            user.PhoneNumb = model.PhoneNumber?.Trim();


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

                    FillLockedFields(model, user);

                    return View("Index", model);
                }

                // Keep Identity username/email in sync
                user.UserName = model.Email.Trim();
            }


            // ========================================================
            // SAVE CHANGES
            // ========================================================

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                FillLockedFields(model, user);

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
            // Profile fields belong to the other form
            ModelState.Remove(nameof(model.FirstName));
            ModelState.Remove(nameof(model.LastName));
            ModelState.Remove(nameof(model.Email));
            ModelState.Remove(nameof(model.PhoneNumber));
            ModelState.Remove(nameof(model.Gender));
            ModelState.Remove(nameof(model.EmployeeNumber));
            ModelState.Remove(nameof(model.IDNumber));

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ReloadProfileInformation(model, user);

                return View("Index", model);
            }


            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Please enter your current password.");

                ReloadProfileInformation(model, user);

                return View("Index", model);
            }


            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Please enter a new password.");

                ReloadProfileInformation(model, user);

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

                ReloadProfileInformation(model, user);

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
        // HELPERS
        // ============================================================

        // Fields the technician is allowed to edit
        private static void FillEditableFields(
            ProfileViewModel model,
            LabUser user)
        {
            model.FirstName = user.FirstName;

            model.LastName = user.LastName;

            model.Email = user.Email ?? "";

            model.PhoneNumber = user.PhoneNumb;
        }

        // Fields that are locked and always come from the database
        private static void FillLockedFields(
            ProfileViewModel model,
            LabUser user)
        {
            model.UserName = user.UserName;

            model.Gender = user.Gender;

            model.EmployeeNumber = user.EmployeeNumber;

            //model.IDNumber = user.IDNumber;

            model.AccountCreated = user.Timestamp_AccountCreated;
        }

        // Used when the password form fails: reset the profile form
        // to the saved values and keep the typed passwords out of it
        private static void ReloadProfileInformation(
            ProfileViewModel model,
            LabUser user)
        {
            FillEditableFields(model, user);
            FillLockedFields(model, user);
        }
    }
}