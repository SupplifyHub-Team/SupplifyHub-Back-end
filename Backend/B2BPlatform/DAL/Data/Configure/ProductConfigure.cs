using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities;
namespace DAL.Data.Configure
{
    public class ProductConfigure : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products"); // Explicitly set table name if different from class name
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(255).IsRequired(true);
            builder.Property(x => x.ProductImageURl).HasColumnName("ProductImageURl").HasColumnType("varchar").IsRequired(true); // Phone can be optional
            builder.Property(x => x.ImagePublicId).HasColumnName("ImagePublicId").HasColumnType("varchar").IsRequired();
            builder.Property(x => x.Description).HasColumnName("Description").HasColumnType("varchar").HasMaxLength(255).IsRequired(true);
            builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired(true);


            builder.HasOne(u => u.Supplier)
                   .WithMany(ur => ur.Products)
                   .HasForeignKey(ur => ur.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.SpecialProduct)
                   .WithOne(ur => ur.Product)
                   .HasForeignKey<SpecialProduct>(ur => ur.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }

}
