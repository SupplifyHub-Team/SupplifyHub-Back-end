using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    // User Management Configurations
    public class UserConfigure : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users"); // Explicitly set table name if different from class name
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.Name).IsUnique(); // Name should be unique
            builder.Property(x => x.Phone).HasColumnName("Phone").HasColumnType("varchar").HasMaxLength(20).IsRequired(); // Phone can be optional
            builder.Property(x => x.EmailVerified).HasColumnName("Verified").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasColumnType("boolean").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.Email).HasColumnName("Email").HasColumnType("varchar").HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique(); // Email should be unique
            builder.Property(x => x.PasswordHash).HasColumnName("PasswordHash").HasColumnType("text").IsRequired(); // Store hash, not plain password
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            builder.HasMany(u => u.UserRoles)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // If user is deleted, their roles are deleted

            builder.HasMany(u => u.UserTokens)
                   .WithOne(ut => ut.User)
                   .HasForeignKey(ut => ut.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // If user is deleted, their tokens are deleted

            builder.HasMany(u => u.UserRequestCategories)
                   .WithOne(ut => ut.User)
                   .HasForeignKey(ut => ut.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // If user is deleted, their tokens are deleted

            builder.HasOne(u => u.JopSeeker)
                   .WithOne(i => i.User)
                   .HasForeignKey<JopSeeker>(i => i.UserId)
                   .IsRequired(false) // A user might not be an 'Individual' initially
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting User if Individual exists

            builder.HasOne(u => u.Supplier)
                   .WithOne(c => c.User)
                   .HasForeignKey<Supplier>(c => c.UserId)
                   .IsRequired(false) // A user might not be associated with a 'Company' directly
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting User if Company exists
        }
    }
}
