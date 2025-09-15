using Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Entities;
// User Management
public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsActive{ get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; } // Renamed from Password_hash for convention
    public DateTime CreatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<UserToken> UserTokens { get; set; }
    public JopSeeker JopSeeker { get; set; } // One-to-one with Individual
    public Supplier Supplier { get; set; } // One-to-one with Company (assuming a User can be associated with one Company directly)
    public ICollection<Order> Orders { get; set; } // Orders placed by the company
    //public ICollection<PasswordResetToken> passwordResetTokens{ get; set; } // Orders placed by the company
    public ICollection<Deal> Deals { get; set; }
    public ICollection<DealDetailsVerification> DealDetailsVerifications { get; set; }
    public ICollection<Review> ReviewsReceived { get; set; }
    public ICollection<Review> ReviewsGiven { get; set; }
    public ICollection<UserRequestCategory> UserRequestCategories { get; set; }
}
