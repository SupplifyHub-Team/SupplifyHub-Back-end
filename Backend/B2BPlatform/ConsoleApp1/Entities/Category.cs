using Enum;
using System.ComponentModel.DataAnnotations;
namespace Entities;

public class Category
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string PhotoURL { get; set; }
    public string ImagePublicId { get; set; }
    public CategoryType CategoryType { get; set; }
    public DateTime CreatedAt { get; set; }
    public CategoryStatus CategoryStatus { get; set; }

    // Navigation properties
    public ICollection<JobPost> JobPosts { get; set; }
    public ICollection<JopSeekerCategoryApply> JopSeekerCategoryApplies { get; set; }
    public ICollection<Order> Orders { get; set; }
    public ICollection<SupplierCategory> SupplierCategorys { get; set; }
}
