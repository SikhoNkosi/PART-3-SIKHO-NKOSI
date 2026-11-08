using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for entering events: participants enrol, and the enrolment is recorded correctly
    public class EnrolmentTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EnrolmentTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Creates an Organiser, an event owned by them, and a category on that event
        private async Task<(HttpClient Organiser, int EventId, int CategoryId)> SetUpEventWithCategoryAsync()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);
            return (organiser, eventId, categoryId);
        }

        // ---------- Enrolling ----------

        [Fact]
        public async Task Enrol_AsParticipant_Returns201AndStatusConfirmed()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Confirmed", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Enrol_IsRecordedCorrectlyInTheDatabase()
        {
            var (_, eventId, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var profile = await participant.GetAsync("/api/users/me");
            var participantId = (await profile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetInt32();

            var enrolmentId = await ScenarioHelpers.EnrolAsync(participant, categoryId);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var saved = await db.Enrolments.Include(e => e.Category).FirstAsync(e => e.EnrolmentId == enrolmentId);
            Assert.Equal(participantId, saved.ParticipantId);
            Assert.Equal(categoryId, saved.CategoryId);
            Assert.Equal(eventId, saved.Category!.EventId);
        }

        [Fact]
        public async Task Enrol_ShowsUpInTheParticipantsOwnEnrolments()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var response = await participant.GetAsync("/api/enrolments/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal(categoryId, body[0].GetProperty("categoryId").GetInt32());
        }

        [Fact]
        public async Task Enrol_InTheSameCategoryTwice_Returns409()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var second = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_InACategoryThatDoesNotExist_Returns404()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync("/api/categories/999999/enrol", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.PostAsync("/api/categories/1/enrol", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MyEnrolments_OnlyShowsTheLoggedInParticipantsOwn()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var participantA = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var participantB = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participantA, categoryId);

            var response = await participantB.GetAsync("/api/enrolments/me");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());
        }

        // ---------- Organiser viewing enrolments ----------

        [Fact]
        public async Task ViewEventEnrolments_ByTheOwningOrganiser_ListsTheParticipant()
        {
            var (organiser, eventId, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal("Test Participant", body[0].GetProperty("participantName").GetString());
        }

        [Fact]
        public async Task ViewEventEnrolments_ByADifferentOrganiser_Returns403()
        {
            var (_, eventId, _) = await SetUpEventWithCategoryAsync();
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ---------- Cancelling ----------

        [Fact]
        public async Task CancelEnrolment_ByTheParticipantWhoMadeIt_Returns204AndItIsGone()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var enrolmentId = await ScenarioHelpers.EnrolAsync(participant, categoryId);

            var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");
            var list = await participant.GetAsync("/api/enrolments/me");

            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
            var body = await list.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task CancelEnrolment_ByADifferentParticipant_Returns403()
        {
            var (_, _, categoryId) = await SetUpEventWithCategoryAsync();
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var otherParticipant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var enrolmentId = await ScenarioHelpers.EnrolAsync(owner, categoryId);

            var response = await otherParticipant.DeleteAsync($"/api/enrolments/{enrolmentId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_ThatDoesNotExist_Returns404()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.DeleteAsync("/api/enrolments/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
