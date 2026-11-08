using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class OrganiserController : OrganiserBaseController
    {
        public OrganiserController(ApiClient api) : base(api)
        {
        }

        // ---------- Dashboard ----------

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var result = await Api.GetAsync<List<OrganiserEventDto>>("api/events/mine");

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new DashboardViewModel());
            }

            var events = result.Data;
            var now = DateTime.Now;

            var model = new DashboardViewModel
            {
                Events = events,
                UpcomingEvents = events
                    .Where(e => e.EventDate >= now)
                    .OrderBy(e => e.EventDate)
                    .Take(5)
                    .ToList(),
                TotalEnrolments = events.Sum(e => e.TotalEnrolments)
            };

            return View(model);
        }

        // ---------- Enrolments for one event ----------

        [HttpGet]
        public async Task<IActionResult> Enrolments(int eventId)
        {
            var (redirect, model) = await BuildPageAsync(eventId);
            if (redirect != null) return redirect;

            return View(model);
        }

        // ---------- Results for one event ----------

        [HttpGet]
        public async Task<IActionResult> Results(int eventId)
        {
            var (redirect, model) = await BuildPageAsync(eventId);
            if (redirect != null) return redirect;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(SaveResultViewModel model)
        {
            var back = RedirectToAction("Results", new { eventId = model.EventId });

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Please check the finish time and position and try again.";
                return back;
            }

            var formats = new[] { @"h\:mm\:ss", @"hh\:mm\:ss" };
            if (!TimeSpan.TryParseExact(model.FinishTime.Trim(), formats, CultureInfo.InvariantCulture, out var finishTime))
            {
                TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 0:52:14.";
                return back;
            }

            var body = new { finishTime = finishTime.ToString("c"), position = model.Position };

            // A result that already exists is updated, a new one is created
            var result = model.ResultId.HasValue
                ? await Api.PutAsync<object>($"api/results/{model.ResultId.Value}", body)
                : await Api.PostAsync<object>($"api/enrolments/{model.EnrolmentId}/result", body);

            var expired = SessionExpired(result);
            if (expired != null) return expired;

            if (result.Success)
                TempData["Success"] = "The result has been saved.";
            else
                TempData["Error"] = result.ErrorMessage;

            return back;
        }

        private async Task<(IActionResult? Redirect, EventEnrolmentsViewModel? Model)> BuildPageAsync(int eventId)
        {
            var (redirect, ev) = await LoadOwnedEventAsync(eventId);
            if (redirect != null) return (redirect, null);

            var result = await Api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");

            var expired = SessionExpired(result);
            if (expired != null) return (expired, null);

            if (!result.Success) TempData["Error"] = result.ErrorMessage;

            return (null, new EventEnrolmentsViewModel
            {
                EventId = ev!.EventId,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                Enrolments = result.Data ?? new List<EventEnrolmentDto>()
            });
        }
    }
}
