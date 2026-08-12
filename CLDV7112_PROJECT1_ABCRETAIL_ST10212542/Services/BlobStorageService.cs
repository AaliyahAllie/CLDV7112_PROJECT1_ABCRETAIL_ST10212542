using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient("product-images");

            // Create container and set access level to public so images can be displayed
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadBlobAsync(string fileName, Stream fileStream)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = GetContentType(fileName) });
            return blobClient.Uri.ToString();
        }

        public async Task DeleteBlobAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };
        }
    }
}
