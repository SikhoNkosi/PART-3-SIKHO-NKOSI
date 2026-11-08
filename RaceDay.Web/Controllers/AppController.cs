using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Base class for controllers that call the API
    public abstract class AppController : Controller
    {
        // If the API says "not logged in" (for example because it was restarted),
        // clear the website session and send the user back to the login page
        protected IActionResult? SessionExpired<T>(ApiResult<T> result)
        {
            if (!result.IsUnauthorized) return null;

            HttpContext.Session.Clear();
            TempData["Error"] = "Your session has expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }
    }
}