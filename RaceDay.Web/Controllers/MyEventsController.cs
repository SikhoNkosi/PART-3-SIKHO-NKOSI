using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireLogin("Participant")]
    public class MyEventsController : AppController
    {
        private readonly ApiClient _api;

        public MyEventsController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
                TempData["Error"] = result.ErrorMessage;

            return View(result.Data ?? new List<MyEnrolmentDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _api.DeleteAsync<object>($"api/enrolments/{id}");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (result.Success)
                TempData["Success"] = "Your enrolment has been cancelled.";
            else
                TempData["Error"] = result.ErrorMessage;

            return RedirectToAction("Index");
        }
    }
}
