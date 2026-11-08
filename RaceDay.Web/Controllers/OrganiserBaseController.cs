using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Everything that inherits from this is only for logged-in Organisers
    [RequireLogin("Organiser")]
    public abstract class OrganiserBaseController : AppController
    {
        protected ApiClient Api { get; }

        protected OrganiserBaseController(ApiClient api)
        {
            Api = api;
        }

        // Loads an event and checks it belongs to the logged-in Organiser.
        // If something is wrong, Redirect says where to send the user instead.
        protected async Task<(IActionResult? Redirect, EventDto? Event)> LoadOwnedEventAsync(int eventId)
        {
            var result = await Api.GetAsync<EventDto>($"api/events/{eventId}");

            var expired = SessionExpired(result);
            if (expired != null) return (expired, null);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage;
                return (RedirectToAction("Dashboard", "Organiser"), null);
            }

            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (result.Data.OrganiserId != userId)
            {
                TempData["Error"] = "You can only manage events that you created.";
                return (RedirectToAction("Dashboard", "Organiser"), null);
            }

            return (null, result.Data);
        }
    }
}
