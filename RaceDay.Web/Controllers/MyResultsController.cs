using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireLogin("Participant")]
    public class MyResultsController : AppController
    {
        private readonly ApiClient _api;

        public MyResultsController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<List<MyResultDto>>("api/results/me");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
                TempData["Error"] = result.ErrorMessage;

            return View(result.Data ?? new List<MyResultDto>());
        }
    }
}
