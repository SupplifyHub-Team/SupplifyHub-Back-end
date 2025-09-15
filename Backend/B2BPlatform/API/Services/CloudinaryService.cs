using API.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace YourNamespace
{
    public class CloudinaryUploadResultDto
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
    }
    public interface ICloudinaryService
    {
        Task<string> UploadPdfAsync(IFormFile file);
        Task<CloudinaryUploadResultDto> UploadPdfForBlogAsync(IFormFile file);
        Task<CloudinaryUploadResultDto> UploadImageAsync(IFormFile file, string? oldPublicId = null);
        Task<bool> DeleteAsync(string publicId);
        Task<bool> DeletePdfAsync(string publicId);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            var cloudinaryOptions = options.Value;
            _cloudinary = new Cloudinary(new Account(
                cloudinaryOptions.CloudName,
                cloudinaryOptions.ApiKey,
                cloudinaryOptions.ApiSecret
            ));
        }
        public async Task<CloudinaryUploadResultDto> UploadPdfForBlogAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = $"pdfs/{Guid.NewGuid()}",
                Overwrite = true
                // هنا مش محتاج تكتب ResourceType = ResourceType.Raw لأن RawUploadParams بيحددها تلقائيًا
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary error: {uploadResult.Error.Message}");

            return new CloudinaryUploadResultDto
            {
                Url = uploadResult.SecureUrl?.AbsoluteUri ?? string.Empty,
                PublicId = uploadResult.PublicId ?? string.Empty
            };
        }

        public async Task<string> UploadPdfAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = $"tax_docs/{Guid.NewGuid()}",
                Overwrite = false,
                //ResourceType = "raw"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary error: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }
        public async Task<CloudinaryUploadResultDto> UploadImageAsync(IFormFile file, string? oldPublicId = null)
        {
            // Delete old if exists
            if (!string.IsNullOrEmpty(oldPublicId))
            {
                await DeleteAsync(oldPublicId);
            }

            await using var stream = file.OpenReadStream();

            var publicId = $"images/{Guid.NewGuid()}"; // Or tie to userId if profile photo

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                Overwrite = true,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary error: {uploadResult.Error.Message}");

            return new CloudinaryUploadResultDto
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId
            };
        }
        public async Task<bool> DeleteAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image // change to Raw for PDFs if needed
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            return deletionResult.Result == "ok";
        }
        public async Task<bool> DeletePdfAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw // change to Raw for PDFs
            };
            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
            return deletionResult.Result == "ok";
        }
    }
}