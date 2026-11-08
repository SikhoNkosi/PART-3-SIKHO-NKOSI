using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for uploading event banners and profile pictures through the API
    public class ImageUploadTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ImageUploadTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Builds a form with one file in a field called "file", like a real browser upload
        private static MultipartFormDataContent ImageForm(
            string fileName = "photo.png", string contentType = "image/png", int size = 200)
        {
            var fileContent = new ByteArrayContent(new byte[size]);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", fileName);
            return form;
        }

        // ---------- Event banners ----------

        [Fact]
        public async Task UploadBanner_ByTheOwner_ReturnsTheUrlAndSavesItOnTheEvent()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageForm());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var url = body.GetProperty("bannerImageUrl").GetString();
            Assert.StartsWith("https://fake-storage.test/", url);

            var check = await organiser.GetAsync($"/api/events/{eventId}");
            var eventBody = await check.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(url, eventBody.GetProperty("bannerImageUrl").GetString());
        }

        [Fact]
        public async Task UploadBanner_ByAnotherOrganiser_Returns403()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);

            var response = await otherOrganiser.PostAsync($"/api/events/{eventId}/banner", ImageForm());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync($"/api/events/{eventId}/banner", ImageForm());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.PostAsync("/api/events/1/banner", ImageForm());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_ForAnEventThatDoesNotExist_Returns404()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PostAsync("/api/events/999999/banner", ImageForm());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WithANonImageFile_Returns400()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync(
                $"/api/events/{eventId}/banner", ImageForm("notes.txt", "text/plain"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WithAFileOverFiveMegabytes_Returns400()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync(
                $"/api/events/{eventId}/banner", ImageForm(size: 5 * 1024 * 1024 + 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WithNoFileAtAll_Returns400()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", new MultipartFormDataContent());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ---------- Profile pictures ----------

        [Fact]
        public async Task UploadProfilePicture_AsParticipant_ShowsUpOnTheirProfile()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync("/api/users/me/profile-picture", ImageForm("me.jpg", "image/jpeg"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var url = body.GetProperty("profilePictureUrl").GetString();
            Assert.StartsWith("https://fake-storage.test/", url);

            var profile = await participant.GetAsync("/api/users/me");
            var profileBody = await profile.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(url, profileBody.GetProperty("profilePictureUrl").GetString());
        }

        [Fact]
        public async Task UploadProfilePicture_AsOrganiser_AlsoWorks()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PostAsync("/api/users/me/profile-picture", ImageForm());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UploadProfilePicture_WhenNotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.PostAsync("/api/users/me/profile-picture", ImageForm());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UploadProfilePicture_WithANonImageFile_Returns400()
        {
            var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync(
                "/api/users/me/profile-picture", ImageForm("notes.txt", "text/plain"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
