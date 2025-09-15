namespace Entities;

public class SupplierCategory
{
    public int SupplierId { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public Supplier Supplier { get; set; }
}
