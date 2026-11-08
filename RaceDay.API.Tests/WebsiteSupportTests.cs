using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for the extra data the Part 3 website needs: event filters, the Organiser dashboard,
    // and richer enrolment and result responses
    public class WebsiteSupportTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public WebsiteSupportTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static object EventBody(string name, string type, string province, DateTime date)
        {
            return new
            {
                eventName = name,
                description = "Test event",
                eventDate = date,
                location = "Cape Town",
                province,
                distance = 10,
                eventType = type,
                routeMapUrl = ""
            };
        }

        private static string NewTag()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static List<string> EventNames(JsonElement array)
        {
            return array.EnumerateArray().Select(e => e.GetProperty("eventName").GetString()!).ToList();
        }

        // ---------- Filters on the event list ----------

        [Fact]
        public async Task GetEvents_FilteredByType_OnlyReturnsThatType()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Run-{tag}", "Run", "Gauteng", DateTime.UtcNow.AddDays(10)));
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Cycle-{tag}", "Cycle", "Gauteng", DateTime.UtcNow.AddDays(10)));
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events?eventType=Cycle");

            var names = EventNames(await response.Content.ReadFromJsonAsync<JsonElement>());
            Assert.Contains($"Cycle-{tag}", names);
            Assert.DoesNotContain($"Run-{tag}", names);
        }

        [Fact]
        public async Task GetEvents_Search_FindsEventsByName()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Marathon-{tag}", "Run", "Gauteng", DateTime.UtcNow.AddDays(10)));
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Other-{tag}", "Run", "Gauteng", DateTime.UtcNow.AddDays(10)));
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync($"/api/events?search=Marathon-{tag}");

            var names = EventNames(await response.Content.ReadFromJsonAsync<JsonElement>());
            Assert.Contains($"Marathon-{tag}", names);
            Assert.DoesNotContain($"Other-{tag}", names);
        }

        [Fact]
        public async Task GetEvents_FilteredByProvince_OnlyReturnsThatProvince()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            await organiser.PostAsJsonAsync("/api/events", EventBody($"WC-{tag}", "Run", "Western Cape", DateTime.UtcNow.AddDays(10)));
            await organiser.PostAsJsonAsync("/api/events", EventBody($"KZN-{tag}", "Run", "KwaZulu-Natal", DateTime.UtcNow.AddDays(10)));
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events?province=KwaZulu-Natal");

            var names = EventNames(await response.Content.ReadFromJsonAsync<JsonElement>());
            Assert.Contains($"KZN-{tag}", names);
            Assert.DoesNotContain($"WC-{tag}", names);
        }

        [Fact]
        public async Task GetEvents_UpcomingOnly_HidesEventsThatAlreadyHappened()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Past-{tag}", "Run", "Gauteng", DateTime.UtcNow.AddDays(-20)));
            await organiser.PostAsJsonAsync("/api/events", EventBody($"Future-{tag}", "Run", "Gauteng", DateTime.UtcNow.AddDays(20)));
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events?upcomingOnly=true");

            var names = EventNames(await response.Content.ReadFromJsonAsync<JsonElement>());
            Assert.Contains($"Future-{tag}", names);
            Assert.DoesNotContain($"Past-{tag}", names);
        }

        // ---------- Organiser dashboard ----------

        [Fact]
        public async Task MyEvents_AsOrganiser_ListsOnlyOwnEventsWithEnrolmentCounts()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser, $"Mine-{tag}");
            await ScenarioHelpers.CreateEventAsync(otherOrganiser, $"Theirs-{tag}");
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var response = await organiser.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var names = EventNames(body);
            Assert.Contains($"Mine-{tag}", names);
            Assert.DoesNotContain($"Theirs-{tag}", names);
            var mine = body.EnumerateArray().First(e => e.GetProperty("eventId").GetInt32() == eventId);
            Assert.Equal(1, mine.GetProperty("totalEnrolments").GetInt32());
            Assert.Equal(1, mine.GetProperty("categoryCount").GetInt32());
        }

        [Fact]
        public async Task MyEvents_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MyEvents_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ---------- Richer enrolments and results ----------

        [Fact]
        public async Task MyEnrolments_IncludeTheEventNameAndCategory()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser, $"Event-{tag}");
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId, "21km");
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var response = await participant.GetAsync("/api/enrolments/me");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal($"Event-{tag}", body[0].GetProperty("eventName").GetString());
            Assert.Equal("21km", body[0].GetProperty("categoryName").GetString());
            Assert.Equal("Confirmed", body[0].GetProperty("status").GetString());
        }

        [Fact]
        public async Task EventEnrolments_ShowTheResultOnceItIsCaptured()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var enrolmentId = await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var before = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
            var beforeBody = await before.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, beforeBody[0].GetProperty("resultId").ValueKind);

            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentId, 5);

            var after = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
            var afterBody = await after.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(5, afterBody[0].GetProperty("position").GetInt32());
        }

        [Fact]
        public async Task MyResults_IncludeEventDetailsAndTheTotalNumberOfFinishers()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var tag = NewTag();
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser, $"Race-{tag}");
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);
            var runnerOne = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var runnerTwo = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var enrolmentOne = await ScenarioHelpers.EnrolAsync(runnerOne, categoryId);
            var enrolmentTwo = await ScenarioHelpers.EnrolAsync(runnerTwo, categoryId);
            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentOne, 1);
            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentTwo, 2);

            var response = await runnerTwo.GetAsync("/api/results/me");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal($"Race-{tag}", body[0].GetProperty("eventName").GetString());
            Assert.Equal(2, body[0].GetProperty("position").GetInt32());
            Assert.Equal(2, body[0].GetProperty("totalFinishers").GetInt32());
        }
    }
}
