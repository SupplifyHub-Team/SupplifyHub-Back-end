using Enum;
using System.ComponentModel;
namespace Entities;

public class SupplierSubscriptionPlan
{
    // Composite key (fluent API will configure this)
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int PlanId { get; set; }
    public int NumberOfProduct { get; set; }
    public int NumberOfSpecialProduct { get; set; }
    public int NumberOfAdvertisement { get; set; }
    public int NumberOfAcceptOrder { get; set; }
    public bool EarlyAccessToOrder { get; set; }
    public bool ShowHigherInSearch { get; set; }    
    public bool CompetitorAndMarketAnalysis { get; set; }   
    public bool ProductVisitsAndPerformanceAnalysis { get; set; }    
    public bool DirectTechnicalSupport { get; set; }   
    public DateTime CreatedAt { get; set; }    
    public DateTime UpdatedAt { get; set; }   
    public PaymentStatus PaymentStatus { get; set; } 
    public string PlanName { get; set; } 
      
    public DateTime StartDate { get; set; }  
    public DateTime EndDate { get; set; }   
    // Navigation properties
    public Supplier Supplier { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; }
}
