using API.ImageResponse;
using Entities;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class APIBaseController : ControllerBase
    {
        protected IUnitOfWork _unitOfWork;
        protected readonly IConfiguration _config;

        public APIBaseController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public APIBaseController(IUnitOfWork unitOfWork,IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _config = configuration;
        }
        // --- GenerateSecureToken ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method generates a secure, tamper-proof token used for authentication and verification purposes, such as email confirmation or password reset.
        /// It creates a payload containing key information (like a user ID, order ID, or email) and a timestamp. It then uses HMAC-SHA256 to generate a unique hash of the payload using a secret key.
        /// Finally, it combines the payload and the hash and encodes the result into a URL-safe Base64 string.
        /// </summary>
        protected string GenerateSecureToken(int? userId = null, int? orderId = null, string? email = null)
        {
            // Get secret from configuration
            var secret = _config["App:TokenSecret"];
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("TokenSecret is missing in configuration");

            // Create payload
            string payload;
            if (email == null)
                payload = $"{userId}|{orderId}|{DateTime.UtcNow.Ticks}";
            else
                payload = $"{email}|{DateTime.UtcNow.Ticks}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Generate HMAC
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(payloadBytes);

            // Combine payload and hash
            var tokenBytes = new byte[payloadBytes.Length + hash.Length];
            Buffer.BlockCopy(payloadBytes, 0, tokenBytes, 0, payloadBytes.Length);
            Buffer.BlockCopy(hash, 0, tokenBytes, payloadBytes.Length, hash.Length);

            return WebEncoders.Base64UrlEncode(tokenBytes);
        }
        
        // --- ValidateToken ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method validates a secure token generated for purposes like email confirmation.
        /// It decodes the URL-safe Base64 token and separates the payload from the HMAC-SHA256 hash.
        /// It then verifies the token's authenticity by recomputing the hash of the payload and comparing it to the original hash. It also validates that the token has not expired based on its timestamp.
        /// If the token is valid and not expired, it returns `true` and extracts the email from the payload. Otherwise, it returns `false`.
        /// </summary>
        protected bool ValidateToken(string token, out string email)
        {
            email = "";
            try
            {
                // Get secret from configuration
                var secret = _config["App:TokenSecret"];
                if (string.IsNullOrEmpty(secret))
                    return false;

                // Decode token
                var tokenBytes = WebEncoders.Base64UrlDecode(token);

                // Extract payload and hash
                var payloadBytes = new byte[tokenBytes.Length - 32];
                var hash = new byte[32];
                Buffer.BlockCopy(tokenBytes, 0, payloadBytes, 0, payloadBytes.Length);
                Buffer.BlockCopy(tokenBytes, payloadBytes.Length, hash, 0, 32);

                // Get payload
                var payload = Encoding.UTF8.GetString(payloadBytes);
                var parts = payload.Split('|');
                if (parts.Length != 2) return false;

                // Validate timestamp
                var timestamp = new DateTime(long.Parse(parts[1]));
                if (DateTime.UtcNow - timestamp > TimeSpan.FromHours(24))
                    return false;

                // Validate HMAC
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedHash = hmac.ComputeHash(payloadBytes);

                // Compare hashes
                if (!computedHash.SequenceEqual(hash))
                    return false;

                email = parts[0];
                return true;
            }
            catch
            {
                return false;
            }
        }

        // --- GetResponseForValidation ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method extracts validation errors from the `ModelState` object. It iterates through the model state entries and, for each key with a validation error, it adds the key and its first error message to a dictionary. This method simplifies returning a clean and structured list of validation errors to the client.
        /// </summary>
        protected Dictionary<string, string> GetResponseForValidation()
        {
            Dictionary<string, string> errors = new();
            foreach (var modelStateKey in ModelState.Keys)
            {
                var modelStateEntry = ModelState[modelStateKey];
                if (modelStateEntry.Errors.Count > 0)
                    errors.Add(modelStateKey, modelStateEntry.Errors.First().ErrorMessage);
            }
            return errors;
        }
        
        // --- ValidatePhoto ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method validates an uploaded image file. It checks for a valid file, ensuring its size does not exceed 5MB and that its file type and extension are either JPG or PNG. Any validation failures are recorded in the `ModelState` object.
        /// </summary>
        protected void ValidatePhoto(IFormFile photo)
        {
            if (photo.Length == 0)
                ModelState.AddModelError("Photo", "يجب ادخال صورة");
            else if (photo.Length > 5 * 1024 * 1024) // 5MB
                ModelState.AddModelError("Photo", " 5MB أقصى حجم ");
            else
            {
                var allowedTypes = new[] { "image/jpeg", "image/png" };
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(photo.FileName)?.ToLowerInvariant();
                var contentType = photo.ContentType?.ToLower();
                if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(contentType) || !allowedTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Photo", " فقط .jpg, .jpeg, or .png صور في شكل");
            }
        }
        // --- ValidatePdf ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method validates an uploaded PDF file.
        /// It checks if the file exists, ensures its size does not exceed 10MB,
        /// and validates that the file type and extension are strictly PDF.
        /// Any validation failures are recorded in the `ModelState` object.
        /// </summary>
        protected void ValidatePdf(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                ModelState.AddModelError("Pdf", "يجب إدخال ملف PDF");
            }
            else if (pdfFile.Length > 5 * 1024 * 1024) // 5MB
            {
                ModelState.AddModelError("Pdf", "أقصى حجم للـ PDF هو 5MB");
            }
            else
            {
                var allowedTypes = new[] { "application/pdf" };
                var allowedExtensions = new[] { ".pdf" };

                var extension = Path.GetExtension(pdfFile.FileName)?.ToLowerInvariant();
                var contentType = pdfFile.ContentType?.ToLower();

                if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(contentType)
                    || !allowedTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Pdf", "يجب رفع ملف PDF فقط");
                }
            }
        }

    }
}