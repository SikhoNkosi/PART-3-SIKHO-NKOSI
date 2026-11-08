using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : AppController
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api)
        {
            _api = api;
        }

        // Anyone can look at an event. Only a logged-in Participant sees the "Enter" buttons.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _api.GetAsync<EventDto>($"api/events/{id}");

            if (!ev.Success || ev.Data == null)
            {
                TempData["Error"] = ev.StatusCode == HttpStatusCode.NotFound
                    ? "We couldn't find that event."
                    : ev.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
            var results = await _api.GetAsync<List<PublicResultDto>>($"api/events/{id}/results");

            var role = HttpContext.Session.GetString(SessionKeys.Role);

            // Work out which categories this Participant has already entered
            var enrolledCategoryIds = new HashSet<int>();
            if (role == "Participant")
            {
                var mine = await _api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");

                var expired = SessionExpired(mine);
                if (expired != null) return expired;

                if (mine.Data != null)
                    enrolledCategoryIds = mine.Data.Select(e => e.CategoryId).ToHashSet();
            }

            return View(new EventDetailsViewModel
            {
                Event = ev.Data,
                Categories = categories.Data ?? new List<CategoryDto>(),
                Results = results.Data ?? new List<PublicResultDto>(),
                EnrolledCategoryIds = enrolledCategoryIds,
                IsLoggedIn = HttpContext.Session.GetInt32(SessionKeys.UserId) != null,
                Role = role
            });
        }

        // Enter an event by choosing a category. The API records the enrolment straight away.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireLogin("Participant")]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var result = await _api.PostAsync<object>($"api/categories/{categoryId}/enrol");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Details", new { id = eventId });
            }

            TempData["Success"] = "You're in! Your enrolment has been recorded.";
            return RedirectToAction("Index", "MyEvents");
        }
    }
}
