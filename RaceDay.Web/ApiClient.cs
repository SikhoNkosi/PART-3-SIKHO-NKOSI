using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    // The names of the values the website keeps in the logged-in user's session
    public static class SessionKeys
    {
        public const string UserId = "UserId";
        public const string Role = "Role";
        public const string FullName = "FullName";
        public const string ApiCookie = "ApiCookie";
    }

    // The outcome of one call to the API: either data, or a friendly error message
    public class ApiResult<T>
    {
        public bool Success { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public T? Data { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    }

    // The only way the website talks to the RaceDay API
    public class ApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public ApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        private ISession? Session => _accessor.HttpContext?.Session;

        public Task<ApiResult<T>> GetAsync<T>(string url) =>
            SendAsync<T>(HttpMethod.Get, url, null);

        public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
            SendAsync<T>(HttpMethod.Post, url, ToJson(body));

        public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
            SendAsync<T>(HttpMethod.Put, url, ToJson(body));

        public Task<ApiResult<T>> DeleteAsync<T>(string url) =>
            SendAsync<T>(HttpMethod.Delete, url, null);

        // Sends an uploaded file to the API as form data in a field called "file"
        public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            return await SendAsync<T>(HttpMethod.Post, url, form);
        }

        // Logs in through the API and also returns the API's session cookie,
        // so the website can send it with every later call for this user
        public async Task<(ApiResult<LoginResponse> Result, string? SessionCookie)> LoginAsync(string email, string password)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
                {
                    Content = ToJson(new { email, password })
                };

                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var message = response.StatusCode == HttpStatusCode.Unauthorized
                        ? "Incorrect email or password."
                        : await ReadFriendlyErrorAsync(response);

                    return (new ApiResult<LoginResponse>
                    {
                        Success = false,
                        StatusCode = response.StatusCode,
                        ErrorMessage = message
                    }, null);
                }

                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

                string? cookie = null;
                if (response.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    var sessionCookie = values.FirstOrDefault(v =>
                        v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));
                    cookie = sessionCookie?.Split(';')[0];
                }

                return (new ApiResult<LoginResponse>
                {
                    Success = true,
                    StatusCode = response.StatusCode,
                    Data = data
                }, cookie);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
            {
                return (Unavailable<LoginResponse>(), null);
            }
        }

        private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url) { Content = content };

                // Act as the logged-in user by sending their API session cookie
                var apiCookie = Session?.GetString(SessionKeys.ApiCookie);
                if (!string.IsNullOrEmpty(apiCookie))
                    request.Headers.Add("Cookie", apiCookie);

                using var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    T? data = default;
                    if (response.StatusCode != HttpStatusCode.NoContent &&
                        response.Content.Headers.ContentLength != 0)
                    {
                        data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                    }

                    return new ApiResult<T>
                    {
                        Success = true,
                        StatusCode = response.StatusCode,
                        Data = data
                    };
                }

                return new ApiResult<T>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    ErrorMessage = await ReadFriendlyErrorAsync(response)
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
            {
                return Unavailable<T>();
            }
        }

        private static StringContent? ToJson(object? body)
        {
            if (body == null) return null;
            return new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        private static ApiResult<T> Unavailable<T>() => new()
        {
            Success = false,
            StatusCode = HttpStatusCode.ServiceUnavailable,
            ErrorMessage = "RaceDay is having trouble reaching its server right now. Please try again in a moment."
        };

        // Turns whatever the API sent back into a message that is safe and friendly to show a user
        private static async Task<string> ReadFriendlyErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        // Messages the API writes on purpose, like { "message": "Email already registered." }
                        if (doc.RootElement.TryGetProperty("message", out var message) &&
                            message.ValueKind == JsonValueKind.String)
                        {
                            return message.GetString()!;
                        }

                        // Validation problems, like { "errors": { "Password": ["..."] } }
                        if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                            errors.ValueKind == JsonValueKind.Object)
                        {
                            var messages = errors.EnumerateObject()
                                .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                                .Where(m => !string.IsNullOrWhiteSpace(m));

                            var joined = string.Join(" ", messages);
                            if (joined.Length > 0) return joined;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to the standard messages below
            }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Some of the information you entered isn't valid. Please check it and try again.",
                HttpStatusCode.Unauthorized => "Please log in to continue.",
                HttpStatusCode.Forbidden => "You don't have permission to do that.",
                HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
                HttpStatusCode.Conflict => "That clashes with something that already exists.",
                _ => "Something went wrong on our side. Please try again in a moment."
            };
        }
    }
}
