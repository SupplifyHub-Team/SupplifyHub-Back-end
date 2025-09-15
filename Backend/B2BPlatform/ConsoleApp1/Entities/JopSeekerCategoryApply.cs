namespace Entities;

public class JopSeekerCategoryApply
{
    // Composite key (fluent API will configure this)
    public int JopSeekerId { get; set; } // Renamed from Individual_id for consistency
    public int CategoryId { get; set; }

    // Navigation properties
    public JopSeeker Individual { get; set; }
    public Category Category { get; set; }
}
