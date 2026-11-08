using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class ManageEventsController : OrganiserBaseController
    {
        public ManageEventsController(ApiClient api) : base(api)
        {
        }

        // ---------- List my events ----------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<OrganiserEventDto>>("api/events/mine");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success) TempData["Error"] = result.ErrorMessage;

            return View(result.Data ?? new List<OrganiserEventDto>());
        }

        // ---------- Create ----------

        [HttpGet]
        public IActionResult Create()
        {
            return View("Form", new EventFormViewModel
            {
                EventDate = DateTime.Today.AddDays(30).AddHours(7),
                Distance = 10
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var result = await Api.PostAsync<EventDto>("api/events", ToApiBody(model));

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View("Form", model);
            }

            // The event exists now. If a banner was chosen, send it to the API as well.
            if (model.Banner != null && model.Banner.Length > 0)
            {
                var banner = await Api.PostFileAsync<object>($"api/events/{result.Data.EventId}/banner", model.Banner);
                if (!banner.Success)
                {
                    TempData["Error"] = $"The event was created, but the banner could not be uploaded: {banner.ErrorMessage}";
                    return RedirectToAction("Edit", new { id = result.Data.EventId });
                }
            }

            TempData["Success"] = "Your event has been created. Now add categories so participants can enter.";
            return RedirectToAction("Index", "ManageCategories", new { eventId = result.Data.EventId });
        }

        // ---------- Edit ----------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (redirect, ev) = await LoadOwnedEventAsync(id);
            if (redirect != null) return redirect;

            return View("Form", new EventFormViewModel
            {
                EventId = ev!.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Province = ev.Province,
                Distance = ev.Distance,
                EventType = ev.EventType,
                RouteMapUrl = ev.RouteMapUrl,
                BannerImageUrl = ev.BannerImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventId = id;

            if (!ModelState.IsValid)
            {
                await FillBannerAsync(model);
                return View("Form", model);
            }

            var result = await Api.PutAsync<EventDto>($"api/events/{id}", ToApiBody(model));

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                await FillBannerAsync(model);
                return View("Form", model);
            }

            if (model.Banner != null && model.Banner.Length > 0)
            {
                var banner = await Api.PostFileAsync<object>($"api/events/{id}/banner", model.Banner);
                if (!banner.Success)
                {
                    TempData["Error"] = $"Your changes were saved, but the banner could not be uploaded: {banner.ErrorMessage}";
                    return RedirectToAction("Edit", new { id });
                }
            }

            TempData["Success"] = "Your event has been updated.";
            return RedirectToAction("Index");
        }

        // ---------- Delete ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await Api.DeleteAsync<object>($"api/events/{id}");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (result.Success)
            {
                TempData["Success"] = "The event has been deleted.";
            }
            else if (result.StatusCode == HttpStatusCode.InternalServerError)
            {
                TempData["Error"] = "This event couldn't be deleted. If participants have already enrolled, it can't be removed.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }

            return RedirectToAction("Index");
        }

        private async Task FillBannerAsync(EventFormViewModel model)
        {
            var current = await Api.GetAsync<EventDto>($"api/events/{model.EventId}");
            model.BannerImageUrl = current.Data?.BannerImageUrl;
        }

        private static object ToApiBody(EventFormViewModel m) => new
        {
            eventName = m.EventName,
            description = m.Description,
            eventDate = m.EventDate,
            location = m.Location,
            province = m.Province,
            distance = m.Distance,
            eventType = m.EventType,
            routeMapUrl = m.RouteMapUrl
        };
    }
}
