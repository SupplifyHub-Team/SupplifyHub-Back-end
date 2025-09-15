using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    // Company Management Configurations
    public class SupplierConfigure : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.CountOfOrderAccepted).HasColumnName("CountOfOrderAccepted").HasColumnType("int").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.NumberOfViews).HasColumnName("NumberOfViews").HasColumnType("int").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.IsConfirmByAdmin).HasColumnName("IsConfirmByAdmin").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.TaxNumberURL).HasColumnName("TaxNumberURL").HasColumnType("varchar").IsRequired();
            builder.Property(x => x.Description).HasColumnName("Description").HasColumnType("varchar").IsRequired();
            builder.Property(x => x.LogoURL).HasColumnName("LogoURL").HasColumnType("varchar").IsRequired();
            builder.Property(x => x.ImagePublicId).HasColumnName("ImagePublicId").HasColumnType("varchar").IsRequired(false);
            builder.Property(x => x.Locations).HasColumnName("Locations").HasColumnType("TEXT[]").IsRequired(false);
            
            // Foreign Key to User (defined in UserConfigure as one-to-one)
            builder.Property(x => x.UserId).HasColumnName("UserId").HasColumnType("int").IsRequired();

            
            builder.HasMany(c => c.JobPosts)
                   .WithOne(j => j.Supplier)
                   .HasForeignKey(j => j.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade); // If company is deleted, its jobs are deleted
            
            builder.HasOne(c => c.SupplierSubscriptionPlan)
                   .WithOne(csp => csp.Supplier)
                   .HasForeignKey<SupplierSubscriptionPlan>(csp => csp.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade); // If company is deleted, its subscriptions are deleted
          
            builder.HasMany(c => c.TempForSupplierSubscriptionPlans)
                   .WithOne(csp => csp.Supplier)
                   .HasForeignKey(csp => csp.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade);
           
            builder.HasMany(c => c.Products)
                    .WithOne(csp => csp.Supplier)
                    .HasForeignKey(csp => csp.SupplierId)
                    .OnDelete(DeleteBehavior.Cascade); // If company is deleted, its Products are deleted

            builder.HasMany(c => c.SupplierCategories)
                    .WithOne(cat => cat.Supplier)
                    .HasForeignKey(cat => cat.SupplierId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SupplierAdvertisements)
                    .WithOne(a => a.Supplier)
                    .HasForeignKey(a => a.SupplierId)
                    .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
