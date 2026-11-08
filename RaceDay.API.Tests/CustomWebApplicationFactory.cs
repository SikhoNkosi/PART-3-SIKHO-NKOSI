using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Tests
{
    // Starts the real API inside the test run, but with a fresh in-memory database
    // instead of SQL Server and a fake image store instead of Azure,
    // so the tests can run on any machine (including GitHub).
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "RaceDayTests-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                services.AddSingleton<IImageStorageService, FakeImageStorageService>();
            });
        }
    }

    // Pretends to upload an image and just returns a made-up web address
    public class FakeImageStorageService : IImageStorageService
    {
        public Task<string> UploadImageAsync(Stream content, string originalFileName, string contentType, string folder)
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            return Task.FromResult($"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid():N}{extension}");
        }
    }
}
