using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class ManageCategoriesController : OrganiserBaseController
    {
        public ManageCategoriesController(ApiClient api) : base(api)
        {
        }

        // ---------- List the categories of one event (and add a new one) ----------

        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var (redirect, ev) = await LoadOwnedEventAsync(eventId);
            if (redirect != null) return redirect;

            var (page, expired) = await BuildPageAsync(ev!, null);
            if (expired != null) return expired;

            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
        {
            var (redirect, ev) = await LoadOwnedEventAsync(model.EventId);
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                var (invalidPage, expiredOnInvalid) = await BuildPageAsync(ev!, model);
                if (expiredOnInvalid != null) return expiredOnInvalid;
                return View("Index", invalidPage);
            }

            var result = await Api.PostAsync<CategoryDto>($"api/events/{model.EventId}/categories", ToApiBody(model));

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                var (failedPage, expiredOnFail) = await BuildPageAsync(ev!, model);
                if (expiredOnFail != null) return expiredOnFail;
                return View("Index", failedPage);
            }

            TempData["Success"] = "The category has been added.";
            return RedirectToAction("Index", new { eventId = model.EventId });
        }

        // ---------- Edit ----------

        [HttpGet]
        public async Task<IActionResult> Edit(int eventId, int categoryId)
        {
            var (redirect, _) = await LoadOwnedEventAsync(eventId);
            if (redirect != null) return redirect;

            var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
            var category = categories.Data?.FirstOrDefault(c => c.CategoryId == categoryId);

            if (category == null)
            {
                TempData["Error"] = "We couldn't find that category.";
                return RedirectToAction("Index", new { eventId });
            }

            return View(new CategoryFormViewModel
            {
                EventId = eventId,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                DistanceKm = category.DistanceKm,
                EntryFee = category.EntryFee,
                MaxParticipants = category.MaxParticipants
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await Api.PutAsync<CategoryDto>($"api/categories/{model.CategoryId}", ToApiBody(model));

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["Success"] = "The category has been updated.";
            return RedirectToAction("Index", new { eventId = model.EventId });
        }

        // ---------- Delete ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int eventId, int categoryId)
        {
            var result = await Api.DeleteAsync<object>($"api/categories/{categoryId}");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (result.Success)
            {
                TempData["Success"] = "The category has been deleted.";
            }
            else if (result.StatusCode == HttpStatusCode.InternalServerError)
            {
                TempData["Error"] = "This category couldn't be deleted. If participants have already enrolled, it can't be removed.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }

            return RedirectToAction("Index", new { eventId });
        }

        private async Task<(CategoriesPageViewModel Page, IActionResult? Expired)> BuildPageAsync(
            EventDto ev, CategoryFormViewModel? form)
        {
            var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories");
            var enrolments = await Api.GetAsync<List<EventEnrolmentDto>>($"api/events/{ev.EventId}/enrolments");

            var expired = SessionExpired(enrolments);

            var page = new CategoriesPageViewModel
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Categories = categories.Data ?? new List<CategoryDto>(),
                EnrolmentCounts = (enrolments.Data ?? new List<EventEnrolmentDto>())
                    .GroupBy(e => e.CategoryId)
                    .ToDictionary(g => g.Key, g => g.Count()),
                NewCategory = form ?? new CategoryFormViewModel { EventId = ev.EventId }
            };

            return (page, expired);
        }

        private static object ToApiBody(CategoryFormViewModel m) => new
        {
            categoryName = m.CategoryName,
            distanceKm = m.DistanceKm,
            entryFee = m.EntryFee,
            maxParticipants = m.MaxParticipants
        };
    }
}
