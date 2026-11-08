using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for race results: Organisers capture them, Participants and the public view them
    public class ResultTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ResultTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Creates an Organiser with an event and category, and a Participant enrolled in it
        private async Task<(HttpClient Organiser, HttpClient Participant, int EventId, int EnrolmentId)> SetUpEnrolmentAsync()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
            var enrolmentId = await ScenarioHelpers.EnrolAsync(participant, categoryId);
            return (organiser, participant, eventId, enrolmentId);
        }

        [Fact]
        public async Task CaptureResult_ByTheEventOwner_Returns201()
        {
            var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();

            var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
            {
                finishTime = "00:52:14",
                position = 12
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(12, body.GetProperty("position").GetInt32());
            Assert.Equal("00:52:14", body.GetProperty("finishTime").GetString());
        }

        [Fact]
        public async Task CaptureResult_ForTheSameEnrolmentTwice_Returns409()
        {
            var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();
            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentId);

            var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
            {
                finishTime = "00:45:00",
                position = 1
            });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_ByAnotherOrganiser_Returns403()
        {
            var (_, _, _, enrolmentId) = await SetUpEnrolmentAsync();
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
            {
                finishTime = "00:50:00",
                position = 1
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_ForAnEnrolmentThatDoesNotExist_Returns404()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", new
            {
                finishTime = "00:50:00",
                position = 1
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResult_ByTheEventOwner_Returns200AndChangesIt()
        {
            var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();
            var resultId = await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentId);

            var response = await organiser.PutAsJsonAsync($"/api/results/{resultId}", new
            {
                finishTime = "00:48:30",
                position = 3
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(3, body.GetProperty("position").GetInt32());
        }

        [Fact]
        public async Task ParticipantCanViewTheirOwnResult()
        {
            var (organiser, participant, _, enrolmentId) = await SetUpEnrolmentAsync();
            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentId, 7);

            var response = await participant.GetAsync("/api/results/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal(7, body[0].GetProperty("position").GetInt32());
        }

        [Fact]
        public async Task ParticipantWithNoResult_SeesAnEmptyList()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.GetAsync("/api/results/me");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task EventResults_ArePublic()
        {
            var (organiser, _, eventId, enrolmentId) = await SetUpEnrolmentAsync();
            await ScenarioHelpers.CaptureResultAsync(organiser, enrolmentId);
            var publicClient = TestHelpers.CreateClient(_factory);

            var response = await publicClient.GetAsync($"/api/events/{eventId}/results");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
            Assert.Equal("Test Participant", body[0].GetProperty("participantName").GetString());
        }
    }
}
