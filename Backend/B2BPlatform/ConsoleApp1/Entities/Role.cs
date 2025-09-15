using Enum;
using System.ComponentModel.DataAnnotations;
namespace Entities;

public class Role
{
    [Key]
    public int Id { get; set; }
    public RoleName Name { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; }
}
