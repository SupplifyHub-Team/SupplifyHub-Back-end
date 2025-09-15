using Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
namespace Entities;

// Company Management
public class Supplier
{
    [Key]
    public int Id { get; set; }
    public bool IsConfirmByAdmin { get; set; }
    public string TaxNumberURL { get; set; }
    public string LogoURL { get; set; }
    public string ImagePublicId { get; set; }
    public string Description { get; set; }
    public List<string> Locations { get; set; }
    public int CountOfOrderAccepted { get; set; }
    public int NumberOfViews { get; set; }
    public ICollection<JobPost> JobPosts { get; set; }
    public SupplierSubscriptionPlan SupplierSubscriptionPlan { get; set; }
    public ICollection<UnconfirmedSupplierSubscriptionPlan> TempForSupplierSubscriptionPlans { get; set; }

    public ICollection<SupplierCategory> SupplierCategories { get; set; }
    public ICollection<Product> Products{ get; set; }
    public ICollection<Deal> Deals{ get; set; }
    public ICollection<SupplierAdvertisement> SupplierAdvertisements { get; set; }
    public ICollection<SupplierSubscriptionPlanArchive> SupplierSubscriptionPlanArchive { get; set; }
    public ICollection<SupplierProductRequest> SupplierProductRequests { get; set; }
    public ICollection<SupplierAcceptOrderRequest> SupplierAcceptOrderRequests { get; set; }
    public ICollection<SupplierAdvertisementRequest> SupplierAdvertisementRequests { get; set; }
    public int UserId { get; set; } 
    public User User { get; set; }


}
