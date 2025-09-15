using Enum;
namespace Entities;

public class SupplierAdvertisementRequest
{
    public Guid Id { get; set; }
    public int SupplierId { get; set; }
    public int RequestedAmount { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    // Navigation property
    public Supplier Supplier { get; set; }
}
