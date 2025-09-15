using Enum;
namespace Entities;

public class Deal
{
    public int Id {get; set; }
    public int OrderId {get; set; }
    public int SupplierId {get; set; }
    public int ClientId {get; set; }
    public DealStatus Status {get; set; } 
    public Order Order { get; set; }
    public User Client { get; set; }
    public Supplier Supplier { get; set; }
    public ICollection<DealDetailsVerification>  DealDetailsVerifications { get; set; }
    public ICollection<Review> Reviews { get; set; }
}
