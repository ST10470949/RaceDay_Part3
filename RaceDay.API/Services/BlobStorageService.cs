using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string? _connectionString;
        private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:ConnectionString"];

            _containerName =
                configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        }

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string originalFileName,
            string contentType,
            string folderName)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "AzureBlobStorage:ConnectionString is missing from configuration. " +
                    "Configure it using User Secrets before uploading images.");
            }

            if (fileStream == null || !fileStream.CanRead)
            {
                throw new ArgumentException(
                    "The uploaded file stream is missing or cannot be read.",
                    nameof(fileStream));
            }

            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                throw new ArgumentException(
                    "The original file name is required.",
                    nameof(originalFileName));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException(
                    "The folder name is required.",
                    nameof(folderName));
            }

            var containerClient = new BlobContainerClient(
                _connectionString,
                _containerName);

            // Create the container if it does not exist.
            // No public access is enabled.
            await containerClient.CreateIfNotExistsAsync();

            // Generate a unique file name to avoid overwriting existing files.
            var extension = Path.GetExtension(originalFileName);
            var blobName = $"{folderName}/{Guid.NewGuid()}{extension}";

            var blobClient = containerClient.GetBlobClient(blobName);

            // Preserve the uploaded file's content type.
            var headers = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType
            };

            // Upload the file to Azure Blob Storage.
            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = headers
                });

            // Return the blob URL for the API to save in the database.
            return blobClient.Uri.ToString();
        }
    }

}
