using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    // Put [RequireLogin] on a controller to make it logged-in only,
    // or [RequireLogin("Organiser")] to make it Organiser only.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireLoginAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string? _role;

        public RequireLoginAttribute(string? role = null)
        {
            _role = role;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            await session.LoadAsync();

            var userId = session.GetInt32(SessionKeys.UserId);
            var role = session.GetString(SessionKeys.Role);

            // Not logged in: go to the login page, and come back here afterwards
            if (userId == null || string.IsNullOrEmpty(role))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // Logged in, but this page belongs to the other role
            if (_role != null && role != _role)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
