using Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Entities;

// Subscription Plans
public class SubscriptionPlan
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } // Renamed from plan_name
   
    [Column(TypeName = "decimal(18,3)")] // Example for decimal precision
    public decimal Price { get; set; }
    public string Description { get; set; }
    public List<string> Pros { get; set; }
    public List<string> Cons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } // Renamed from update_at
    public int Duration { get; set; } // Duration in days/months etc.

    // Navigation properties
    public ICollection<SupplierSubscriptionPlan> SupplierSubscriptionPlans { get; set; }
    public ICollection<UnconfirmedSupplierSubscriptionPlan> TempForSupplierSubscriptionPlans { get; set; }
    public ICollection<SupplierSubscriptionPlanArchive> SupplierSubscriptionPlanArchive { get; set; }

}
