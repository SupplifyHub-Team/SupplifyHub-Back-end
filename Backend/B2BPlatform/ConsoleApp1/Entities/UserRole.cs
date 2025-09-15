namespace Entities;

public class UserRole
{
    // Composite key (fluent API will configure this)
    public int UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public User User { get; set; }
    public Role Role { get; set; }
}
