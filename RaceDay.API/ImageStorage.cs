using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDay.API.Services
{
    // Anything that can store an image and give back its web address.
    // The real one uses Azure Blob Storage. The tests use a fake one.
    public interface IImageStorageService
    {
        Task<string> UploadImageAsync(Stream content, string originalFileName, string contentType, string folder);
    }

    // Stores images in the Azure Blob Storage container named in the settings
    public class BlobImageStorageService : IImageStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobImageStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var containerName = config["AzureStorage:ContainerName"] ?? "raceday-images";

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not set. Add it to User Secrets (or an environment variable).");

            _container = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadImageAsync(Stream content, string originalFileName, string contentType, string folder)
        {
            await _container.CreateIfNotExistsAsync();

            // A random name stops two uploads with the same file name overwriting each other
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
            var blob = _container.GetBlobClient(blobName);

            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            return blob.Uri.ToString();
        }
    }

    // The rules every uploaded image must pass
    public static class ImageUploadRules
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Returns an error message, or null if the file is fine
        public static string? Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Please choose an image file to upload.";

            if (file.Length > MaxBytes)
                return "The image is too large. The maximum size is 5 MB.";

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return "Only JPG, PNG or WEBP images are allowed.";

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return "Only JPG, PNG or WEBP images are allowed.";

            return null;
        }
    }
}
