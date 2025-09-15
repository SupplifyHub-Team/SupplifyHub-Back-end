using System.ComponentModel.DataAnnotations;
namespace Entities;

// Individual User (for JobPost applications)
public class JopSeeker
{
    [Key]
    public int UserId { get; set; } // Assuming UserId is also the primary key here, one-to-one with User
    public string ResumePath { get; set; }

    // Navigation property (one-to-one)
    public User User { get; set; }

    // Navigation properties
    public ICollection<JopSeekerCategoryApply> JopSeekerCategoryApplies { get; set; }
}
