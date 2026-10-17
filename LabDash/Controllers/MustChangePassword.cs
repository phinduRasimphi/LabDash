using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabDash.Filters
{
    // Redirects any signed-in user with MustChangePassword = true
    // to the Change Password page, until they change it.
    public class MustChangePasswordFilter : IAsyncActionFilter
    {
        private readonly UserManager<LabUser> _userManager;

        public MustChangePasswordFilter(UserManager<LabUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var user = await _userManager.GetUserAsync(http.User);

            if (user != null && user.MustChangePassword)
            {
                var path = (http.Request.Path.Value ?? "").ToLowerInvariant();

                // Paths we must NOT redirect away from, or the user
                // can never change their password or log out.
                bool isAllowed =
                    path.StartsWith("/account/changepassword") ||
                    path.StartsWith("/account/logout") ||
                    path.StartsWith("/account/login") ||
                    path.StartsWith("/identity/account/manage/changepassword") ||
                    path.StartsWith("/identity/account/logout") ||
                    path.StartsWith("/css") ||
                    path.StartsWith("/js") ||
                    path.StartsWith("/lib") ||
                    path.StartsWith("/images") ||
                    path.StartsWith("/favicon");

                if (!isAllowed)
                {
                    context.Result = new RedirectToActionResult(
                        "ChangePassword",
                        "Account",
                        null);
                    return;
                }
            }

            await next();
        }
    }
}