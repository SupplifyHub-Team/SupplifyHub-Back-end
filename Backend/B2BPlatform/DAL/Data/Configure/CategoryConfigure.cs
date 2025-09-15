using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class CategoryConfigure : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(100).IsRequired();
            builder.Property(x => x.PhotoURL).HasColumnName("PhotoURL").HasColumnType("varchar").IsRequired();
            builder.Property(x => x.ImagePublicId).HasColumnName("ImagePublicId").HasColumnType("varchar").IsRequired();
            builder.HasIndex(x => x.Name).IsUnique(); // Category names should be unique
            builder.Property(x => x.CategoryType).HasConversion(new EnumToStringConverter<CategoryType>()).HasColumnName("CategoryType").HasColumnType("varchar").HasMaxLength(15).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(x => x.CategoryStatus).HasColumnName("CategoryStatus").HasConversion(new EnumToStringConverter<CategoryStatus>()).HasDefaultValue(CategoryStatus.Pending).HasColumnType("varchar").HasMaxLength(15).IsRequired();


            builder.HasMany(cat => cat.JobPosts)
                   .WithOne(j => j.Category)
                   .HasForeignKey(j => j.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Already defined in JobConfigure, but good to have inverse here

            builder.HasMany(cat => cat.JopSeekerCategoryApplies)
                   .WithOne(ica => ica.Category)
                   .HasForeignKey(ica => ica.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if individuals applied to it

            builder.HasMany(cat => cat.Orders)
                   .WithOne(o => o.Category)
                   .HasForeignKey(o => o.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if orders are associated            builder.HasMany(cat => cat.Orders)
 
            builder.HasMany(cat => cat.SupplierCategorys)
               .WithOne(o => o.Category)
               .HasForeignKey(o => o.CategoryId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if CompanyCategorys are associated            builder.HasMany(cat => cat.CompanyCategorys)
        }
    }

}
