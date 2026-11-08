using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using Xunit;

namespace RaceDay.API.Tests
{
    // Tests for registration, login, logout and the server-side session
    public class AuthenticationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthenticationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ---------- Registration ----------

        [Fact]
        public async Task Register_AsOrganiser_Returns201Created()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail(), "Organiser");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_AsParticipant_Returns201Created()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail(), "Participant");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidRole_Returns400BadRequest()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail(), "Admin");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithTooShortPassword_Returns400BadRequest()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail(), "Participant", "123");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409Conflict()
        {
            var client = TestHelpers.CreateClient(_factory);
            var email = TestHelpers.UniqueEmail();
            await TestHelpers.RegisterAsync(client, email, "Participant");

            var response = await TestHelpers.RegisterAsync(client, email, "Participant");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_StoresHashedPassword_NotPlainText()
        {
            var client = TestHelpers.CreateClient(_factory);
            var email = TestHelpers.UniqueEmail();
            await TestHelpers.RegisterAsync(client, email, "Participant", "Password1");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);

            Assert.NotEqual("Password1", user.PasswordHash);
            Assert.StartsWith("$2", user.PasswordHash); // BCrypt hashes always start with $2
        }

        // ---------- Login ----------

        [Fact]
        public async Task Login_WithCorrectCredentials_Returns200AndTheUsersRole()
        {
            var client = TestHelpers.CreateClient(_factory);
            var email = TestHelpers.UniqueEmail();
            await TestHelpers.RegisterAsync(client, email, "Organiser");

            var response = await TestHelpers.LoginAsync(client, email);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Organiser", body.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401Unauthorized()
        {
            var client = TestHelpers.CreateClient(_factory);
            var email = TestHelpers.UniqueEmail();
            await TestHelpers.RegisterAsync(client, email, "Participant");

            var response = await TestHelpers.LoginAsync(client, email, "WrongPassword9");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401Unauthorized()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await TestHelpers.LoginAsync(client, TestHelpers.UniqueEmail());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ---------- Server-side session ----------

        [Fact]
        public async Task Profile_WhenLoggedIn_Returns200AndTheSessionRememberedTheUser()
        {
            var client = TestHelpers.CreateClient(_factory);
            var email = TestHelpers.UniqueEmail();
            await TestHelpers.RegisterAsync(client, email, "Participant");
            await TestHelpers.LoginAsync(client, email);

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(email, body.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Profile_SessionStoresTheRole_ParticipantStaysParticipant()
        {
            var client = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var response = await client.GetAsync("/api/users/me");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Participant", body.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Profile_WhenNotLoggedIn_Returns401Unauthorized()
        {
            var client = TestHelpers.CreateClient(_factory);

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_ClearsTheSession_ProfileThenReturns401()
        {
            var client = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

            var logout = await client.PostAsync("/api/auth/logout", null);
            var profile = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_WhenLoggedIn_ChangesTheName()
        {
            var client = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

            var update = await client.PutAsJsonAsync("/api/users/me", new
            {
                fullName = "Updated Name",
                phoneNumber = "0831112222"
            });

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var body = await update.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Updated Name", body.GetProperty("fullName").GetString());
        }
    }
}