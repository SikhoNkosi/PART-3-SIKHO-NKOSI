using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests that the wrong role is rejected, and that logged-out users are rejected
    public class RoleEnforcementTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RoleEnforcementTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ---------- A Participant must not be able to use Organiser features ----------

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsJsonAsync("/api/events", ScenarioHelpers.NewEventBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PutAsJsonAsync($"/api/events/{eventId}", ScenarioHelpers.NewEventBody("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsParticipant_Returns403AndTheEventStays()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var delete = await participant.DeleteAsync($"/api/events/{eventId}");
            var check = await organiser.GetAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        }

        [Fact]
        public async Task AddCategory_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", ScenarioHelpers.NewCategoryBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ViewEventEnrolments_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsJsonAsync("/api/enrolments/1/result", new
            {
                finishTime = "00:50:00",
                position = 1
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ---------- An Organiser must not be able to use Participant features ----------

        [Fact]
        public async Task Enrol_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);

            var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ViewMyEnrolments_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.GetAsync("/api/enrolments/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ViewMyResults_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.GetAsync("/api/results/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.DeleteAsync("/api/enrolments/1");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ---------- Not logged in ----------

        [Fact]
        public async Task AddCategory_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.PostAsJsonAsync("/api/events/1/categories", ScenarioHelpers.NewCategoryBody());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/users/me")]
        [InlineData("/api/enrolments/me")]
        [InlineData("/api/results/me")]
        [InlineData("/api/events/1/enrolments")]
        public async Task ProtectedEndpoints_WhenNotLoggedIn_Return401(string url)
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/events")]
        [InlineData("/api/events/1/results")]
        public async Task PublicEndpoints_WorkWithoutLogin(string url)
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
