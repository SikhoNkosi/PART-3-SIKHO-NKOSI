using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for event management: view, create, update and delete
    public class EventTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ---------- Viewing events (public) ----------

        [Fact]
        public async Task GetEvents_WithoutLogin_Returns200()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEventById_WhenItExists_Returns200WithTheEvent()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser, "Soweto Test Run");
            var publicClient = TestHelpers.CreateClient(_factory);

            var response = await publicClient.GetAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Soweto Test Run", body.GetProperty("eventName").GetString());
        }

        [Fact]
        public async Task GetEventById_WhenItDoesNotExist_Returns404()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ---------- Creating events ----------

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201AndSavesTheEventTypeAndDistance()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", ScenarioHelpers.NewEventBody("Cycle Tour"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Cycle Tour", body.GetProperty("eventName").GetString());
            Assert.Equal("Run", body.GetProperty("eventType").GetString());
            Assert.Equal(10m, body.GetProperty("distance").GetDecimal());
        }

        [Fact]
        public async Task CreateEvent_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.PostAsJsonAsync("/api/events", ScenarioHelpers.NewEventBody());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ---------- Updating events ----------

        [Fact]
        public async Task UpdateEvent_ByTheOwner_Returns200AndChangesTheEvent()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
            {
                eventName = "Renamed Event",
                description = "Changed",
                eventDate = DateTime.UtcNow.AddDays(60),
                location = "Durban",
                province = "KwaZulu-Natal",
                distance = 21,
                eventType = "Cycle",
                routeMapUrl = ""
            });

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var check = await organiser.GetAsync($"/api/events/{eventId}");
            var body = await check.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Renamed Event", body.GetProperty("eventName").GetString());
            Assert.Equal("Cycle", body.GetProperty("eventType").GetString());
            Assert.Equal(21m, body.GetProperty("distance").GetDecimal());
        }

        [Fact]
        public async Task UpdateEvent_ByADifferentOrganiser_Returns403()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);

            var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", ScenarioHelpers.NewEventBody("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_ThatDoesNotExist_Returns404()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PutAsJsonAsync("/api/events/999999", ScenarioHelpers.NewEventBody());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ---------- Deleting events ----------

        [Fact]
        public async Task DeleteEvent_ByTheOwner_Returns204AndTheEventIsGone()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
            var check = await organiser.GetAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_ByADifferentOrganiser_Returns403AndTheEventStays()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);

            var delete = await otherOrganiser.DeleteAsync($"/api/events/{eventId}");
            var check = await owner.GetAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_ThatDoesNotExist_Returns404()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.DeleteAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
