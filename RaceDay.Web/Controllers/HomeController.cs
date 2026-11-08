using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;

        public HomeController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? search, string? eventType, string? province)
        {
            // Organisers start on their dashboard
            if (HttpContext.Session.GetString(SessionKeys.Role) == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");

            // Only upcoming events, narrowed down by whatever filters were chosen
            var query = new List<string> { "upcomingOnly=true" };
            if (!string.IsNullOrWhiteSpace(search))
                query.Add("search=" + Uri.EscapeDataString(search.Trim()));
            if (!string.IsNullOrWhiteSpace(eventType))
                query.Add("eventType=" + Uri.EscapeDataString(eventType));
            if (!string.IsNullOrWhiteSpace(province))
                query.Add("province=" + Uri.EscapeDataString(province));

            var result = await _api.GetAsync<List<EventDto>>("api/events?" + string.Join("&", query));

            if (!result.Success)
                TempData["Error"] = result.ErrorMessage;

            return View(new HomeViewModel
            {
                Events = result.Data ?? new List<EventDto>(),
                Search = search,
                EventType = eventType,
                Province = province
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
