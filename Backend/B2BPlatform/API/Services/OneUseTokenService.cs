// Add these using statements at the top of your controller
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Add this class for token data (put this outside your controller class)
public class TokenData
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Nonce { get; set; }
}

// TokenService.cs - Create this in your Services folder
/// <summary>
/// Service for generating and validating secure encrypted tokens using HMAC-SHA256
/// This service provides:
/// 1. Generation of secure tokens containing order data, user information, and expiration date
/// 2. Digital signature verification to ensure token authenticity
/// 3. Validation of token integrity and prevention of reuse
/// 4. Prevention of token replay attacks (using HashSet for temporary storage)
/// 
/// Important Notes:
/// - In production environments, replace HashSet with Redis or distributed cache
/// - Change the default secret key in production environment
/// - Tokens are URL-encoded for safe use in email links
/// </summary>
public class TokenService
{
    private readonly string _secretKey;
    private static readonly HashSet<string> _usedTokens = new HashSet<string>(); // In production, use Redis or distributed cache

    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["TokenSecret"] ?? "your-super-secret-key-change-this-in-production";
    }

    public string GenerateSecureToken(int orderId, int userId, DateTime expiresAt)
    {
        var tokenData = new TokenData
        {
            OrderId = orderId,
            UserId = userId,
            ExpiresAt = expiresAt,
            Nonce = Guid.NewGuid().ToString()
        };

        var tokenJson = JsonSerializer.Serialize(tokenData);
        var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
        var base64Token = Convert.ToBase64String(tokenBytes);

        // Create HMAC signature
        var signature = ComputeHmacSignature(base64Token, _secretKey);
        var finalToken = $"{base64Token}.{signature}";

        // URL encode to prevent issues with email clients
        return Uri.EscapeDataString(finalToken);
    }

    private string ComputeHmacSignature(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    public TokenData ValidateAndParseToken(string token)
    {
        try
        {
            // URL decode first
            token = Uri.UnescapeDataString(token);

            // Check if token was already used
            if (_usedTokens.Contains(token))
                return null;

            var parts = token.Split('.');
            if (parts.Length != 2)
                return null;

            var base64Token = parts[0];
            var signature = parts[1];

            // Verify signature
            var expectedSignature = ComputeHmacSignature(base64Token, _secretKey);
            if (signature != expectedSignature)
                return null;

            // Decode token data
            var tokenBytes = Convert.FromBase64String(base64Token);
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<TokenData>(tokenJson);
            

            // Check expiration
            var expiresAt = tokenData?.ExpiresAt;
            if (expiresAt < DateTime.UtcNow)
                return null;

            // Mark token as used
            _usedTokens.Add(token);

            return tokenData;
        }
        catch
        {
            return null;
        }
    }
}