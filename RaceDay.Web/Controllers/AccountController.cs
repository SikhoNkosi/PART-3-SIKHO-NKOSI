using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _api;

        public AccountController(ApiClient api)
        {
            _api = api;
        }

        // ---------- Log in ----------

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Already logged in? Go home.
            if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (result, cookie) = await _api.LoginAsync(model.Email, model.Password);

            if (!result.Success || result.Data == null || string.IsNullOrEmpty(cookie))
            {
                ModelState.AddModelError(string.Empty,
                    result.Success
                        ? "We couldn't start your session. Please try again."
                        : result.ErrorMessage);
                return View(model);
            }

            // Start a fresh website session that remembers who this is
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);
            HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);
            HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);
            HttpContext.Session.SetString(SessionKeys.ApiCookie, cookie);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ---------- Register ----------

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.PostAsync<object>("api/auth/register", new
            {
                fullName = model.FullName,
                email = model.Email,
                password = model.Password,
                role = model.Role,
                phoneNumber = model.PhoneNumber
            });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["Success"] = "Your account has been created. You can log in now.";
            return RedirectToAction("Login");
        }

        // ---------- Log out ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // End the session on the API too, then clear the website session
            await _api.PostAsync<object>("api/auth/logout");
            HttpContext.Session.Clear();

            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        // ---------- Wrong role ----------

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
