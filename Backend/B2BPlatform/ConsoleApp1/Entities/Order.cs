using Enum;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Models.Entities;
using System.ComponentModel.DataAnnotations;
namespace Entities;

public class Order
{
    [Key]
    public int Id { get; set; }
    //public string DescriptionAndQuantity { get; set; }
    public string RequiredLocation { get; set; }
    //public int Quantity { get; set; }
    public int NumSuppliersDesired { get; set; } // Renamed from num_suppliers_desired
    public string ContactPersonName { get; set; }
    public string ContactPersonNumber { get; set; }

    public OrderStatus OrderStatus { get; set; }
    public DateTime CreatedAt { get; set; } 
    public DateTime Deadline { get; set; }

    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public User User { get; set; }
    public Category Category { get; set; }
    public Deal Deal { get; set; }
    // Navigation property (One-to-Many)
    public ICollection<OrderItem> Items { get; set; }
}
