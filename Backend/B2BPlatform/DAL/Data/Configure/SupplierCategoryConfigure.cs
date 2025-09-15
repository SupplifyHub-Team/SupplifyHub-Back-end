using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    public class SupplierCategoryConfigure : IEntityTypeConfiguration<SupplierCategory>
    {
        public void Configure(EntityTypeBuilder<SupplierCategory> builder)
        {
            builder.ToTable("SupplierCategories");
            builder.HasKey(x => new { x.SupplierId , x.CategoryId }); // Composite primary key

            // Foreign Keys
            builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired();
            builder.Property(x => x.CategoryId).HasColumnName("CategoryId").HasColumnType("int").IsRequired();

            // Foreign Key relationships are defined in CompanyConfigure and CategoryConfigure
        }
    }

}
