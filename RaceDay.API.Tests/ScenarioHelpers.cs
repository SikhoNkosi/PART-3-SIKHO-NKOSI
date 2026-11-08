using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.API.Tests
{
    // Shortcuts that set up common situations (an event, a category, an enrolment, a result)
    public static class ScenarioHelpers
    {
        public static object NewEventBody(string name = "Test Run")
        {
            return new
            {
                eventName = name,
                description = "A test event",
                eventDate = DateTime.UtcNow.AddDays(30),
                location = "Cape Town",
                province = "Western Cape",
                distance = 10,
                eventType = "Run",
                routeMapUrl = ""
            };
        }

        public static object NewCategoryBody(string name = "10km Run")
        {
            return new
            {
                categoryName = name,
                distanceKm = 10,
                entryFee = 150,
                maxParticipants = 100
            };
        }

        // An Organiser creates an event and gets back its id
        public static async Task<int> CreateEventAsync(HttpClient organiser, string name = "Test Run")
        {
            var response = await organiser.PostAsJsonAsync("/api/events", NewEventBody(name));
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("eventId").GetInt32();
        }

        // An Organiser adds a category to an event and gets back its id
        public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "10km Run")
        {
            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", NewCategoryBody(name));
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("categoryId").GetInt32();
        }

        // A Participant enrols in a category and gets back the enrolment id
        public static async Task<int> EnrolAsync(HttpClient participant, int categoryId)
        {
            var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("enrolmentId").GetInt32();
        }

        // An Organiser captures a result for an enrolment and gets back the result id
        public static async Task<int> CaptureResultAsync(HttpClient organiser, int enrolmentId, int position = 12)
        {
            var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
            {
                finishTime = "00:52:14",
                position
            });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("resultId").GetInt32();
        }
    }
}
