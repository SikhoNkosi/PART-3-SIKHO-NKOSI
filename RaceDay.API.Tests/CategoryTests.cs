using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for event categories (for example 10km or Under 20)
    public class CategoryTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CategoryTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AddCategory_ByTheEventOwner_Returns201()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ScenarioHelpers.NewCategoryBody("Under 20"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Under 20", body.GetProperty("categoryName").GetString());
        }

        [Fact]
        public async Task AddCategory_ToAnotherOrganisersEvent_Returns403()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);

            var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ScenarioHelpers.NewCategoryBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCategories_IsPublicAndListsTheEventsCategories()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            await ScenarioHelpers.CreateCategoryAsync(organiser, eventId, "5km Fun Run");
            await ScenarioHelpers.CreateCategoryAsync(organiser, eventId, "10km Run");
            var publicClient = TestHelpers.CreateClient(_factory);

            var response = await publicClient.GetAsync($"/api/events/{eventId}/categories");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, body.GetArrayLength());
        }

        [Fact]
        public async Task GetCategories_ForAnEventThatDoesNotExist_Returns404()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/events/999999/categories");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_ByTheOwner_Returns200AndChangesIt()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);

            var response = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new
            {
                categoryName = "21km Half Marathon",
                distanceKm = 21,
                entryFee = 250,
                maxParticipants = 200
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("21km Half Marathon", body.GetProperty("categoryName").GetString());
        }

        [Fact]
        public async Task UpdateCategory_ByAnotherOrganiser_Returns403()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(owner, eventId);

            var response = await otherOrganiser.PutAsJsonAsync($"/api/categories/{categoryId}", ScenarioHelpers.NewCategoryBody("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ByTheOwner_Returns204()
        {
            var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(organiser);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(organiser, eventId);

            var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ByAnotherOrganiser_Returns403()
        {
            var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var otherOrganiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
            var eventId = await ScenarioHelpers.CreateEventAsync(owner);
            var categoryId = await ScenarioHelpers.CreateCategoryAsync(owner, eventId);

            var response = await otherOrganiser.DeleteAsync($"/api/categories/{categoryId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
