using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireLogin]
    public class ProfileController : AppController
    {
        private readonly ApiClient _api;

        public ProfileController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<ProfileDto>("api/users/me");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            return View(ToViewModel(result.Data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // The read-only bits are not posted back, so fetch them again for the page
                var current = await _api.GetAsync<ProfileDto>("api/users/me");
                var expiredOnReload = SessionExpired(current);
                if (expiredOnReload != null) return expiredOnReload;

                if (current.Data != null)
                {
                    model.Email = current.Data.Email;
                    model.Role = current.Data.Role;
                    model.ProfilePictureUrl = current.Data.ProfilePictureUrl;
                }

                return View("Index", model);
            }

            var result = await _api.PutAsync<ProfileDto>("api/users/me", new
            {
                fullName = model.FullName,
                phoneNumber = model.PhoneNumber
            });

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index");
            }

            // Keep the name in the menu up to date
            HttpContext.Session.SetString(SessionKeys.FullName, model.FullName);

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            if (picture == null || picture.Length == 0)
            {
                TempData["Error"] = "Please choose a picture first.";
                return RedirectToAction("Index");
            }

            // The website never talks to Azure. It sends the file to the API, which stores it.
            var result = await _api.PostFileAsync<object>("api/users/me/profile-picture", picture);

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Your profile picture has been updated.";
            return RedirectToAction("Index");
        }

        private static ProfileViewModel ToViewModel(ProfileDto dto) => new ProfileViewModel
        {
            Email = dto.Email,
            Role = dto.Role,
            ProfilePictureUrl = dto.ProfilePictureUrl,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber
        };
    }
}
