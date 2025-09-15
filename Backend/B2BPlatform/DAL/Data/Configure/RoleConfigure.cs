using Entities;
using Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class RoleConfigure : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasConversion(new EnumToStringConverter<RoleName>()).HasColumnType("varchar").HasMaxLength(20).IsRequired();

            builder.HasIndex(x => x.Name).IsUnique(); // Role names should be unique

            // Relationships
            builder.HasMany(r => r.UserRoles)
                   .WithOne(ur => ur.Role)
                   .HasForeignKey(ur => ur.RoleId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a role if users are assigned to it
        }
    }

}
