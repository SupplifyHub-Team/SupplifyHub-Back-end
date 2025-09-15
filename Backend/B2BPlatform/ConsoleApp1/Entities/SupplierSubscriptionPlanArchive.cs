using Enum;
namespace Entities;

public class SupplierSubscriptionPlanArchive
{
    public Guid Id { get; set; }
    public int SupplierId { get; set; }
    public int PlanId { get; set; }
    public int NumberOfProduct { get; set; }
    public int NumberOfSpecialProduct { get; set; }
    public int NumberOfAdvertisement { get; set; }
    public int NumberOfAcceptOrder { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string PlanName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ArchivedAt { get; set; }
    public Supplier Supplier { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; }
}
