using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.API.Tests
{
    // Small shortcuts used by all the test classes
    public static class TestHelpers
    {
        public const string DefaultPassword = "Password1";

        // Every test uses its own email so tests never clash with each other
        public static string UniqueEmail(string prefix = "user")
        {
            return $"{prefix}-{Guid.NewGuid():N}@test.com";
        }

        // Each client has its own cookie jar, so each client is its own separate logged-in session
        public static HttpClient CreateClient(CustomWebApplicationFactory factory)
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        }

        public static Task<HttpResponseMessage> RegisterAsync(
            HttpClient client, string email, string role, string password = DefaultPassword)
        {
            return client.PostAsJsonAsync("/api/auth/register", new
            {
                fullName = "Test " + role,
                email,
                password,
                role,
                phoneNumber = "0821234567"
            });
        }

        public static Task<HttpResponseMessage> LoginAsync(
            HttpClient client, string email, string password = DefaultPassword)
        {
            return client.PostAsJsonAsync("/api/auth/login", new { email, password });
        }

        // Registers a brand new user, logs them in, and returns a client that is already logged in
        public static async Task<HttpClient> CreateLoggedInClientAsync(
            CustomWebApplicationFactory factory, string role)
        {
            var client = CreateClient(factory);
            var email = UniqueEmail(role.ToLower());

            var register = await RegisterAsync(client, email, role);
            register.EnsureSuccessStatusCode();

            var login = await LoginAsync(client, email);
            login.EnsureSuccessStatusCode();

            // Parse token from login response and set Authorization header for subsequent requests
            var body = await login.Content.ReadFromJsonAsync<JsonElement>();
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("token", out var tkn))
            {
                var token = tkn.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }

            return client;
        }
    }
}
