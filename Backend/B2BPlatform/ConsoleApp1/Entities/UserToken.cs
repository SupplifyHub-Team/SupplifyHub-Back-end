using System.ComponentModel.DataAnnotations;
namespace Entities;

public class UserToken
{
    [Key]
    public Guid Id { get; set; } // Assuming a unique ID for each token
    public bool IsRevoked { get; set; }
    public DateTime? ExpiresAt { get; set; } // Nullable if not all tokens expire
    public string Token { get; set; }

    // Foreign Key
    public int UserId { get; set; }
    // Navigation property
    public User User { get; set; }
}
