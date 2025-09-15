using System.ComponentModel.DataAnnotations;
namespace Entities;

// JobPost & Category
public class JobPost
{
    [Key]
    public int Id { get; set; }
    public DateTime PostedAt { get; set; }
    public string HrEmail { get; set; }
    public bool IsActive { get; set; }

    // Foreign Keys
    public int SupplierId { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; }
    public Category Category { get; set; }
}
