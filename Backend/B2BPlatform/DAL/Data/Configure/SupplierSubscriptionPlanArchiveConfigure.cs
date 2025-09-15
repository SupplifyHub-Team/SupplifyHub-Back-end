using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class SupplierSubscriptionPlanArchiveConfigure : IEntityTypeConfiguration<SupplierSubscriptionPlanArchive>
    {
        public void Configure(EntityTypeBuilder<SupplierSubscriptionPlanArchive> builder)
        {
            builder.ToTable("SupplierSubscriptionPlanArchives");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("Id")
                   .ValueGeneratedOnAdd();

            builder.HasOne(x => x.Supplier)
                   .WithMany(s=>s.SupplierSubscriptionPlanArchive) // No navigation property on the Supplier side for the archive
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete, as archives are historical records

            builder.HasOne(x => x.SubscriptionPlan)
                   .WithMany(p=> p.SupplierSubscriptionPlanArchive) // No navigation property on the SubscriptionPlan side for the archive
                   .HasForeignKey(x => x.PlanId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure properties and column mappings
            builder.Property(x => x.SupplierId).HasColumnType("int")
                   .HasColumnName("SupplierId")
                   .IsRequired();

            builder.Property(x => x.PlanId)
                   .HasColumnName("PlanId").HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.NumberOfProduct)
                   .HasColumnName("NumberOfProduct").HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.NumberOfSpecialProduct)
                   .HasColumnName("NumberOfSpecialProduct").HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.NumberOfAdvertisement)
                   .HasColumnName("NumberOfAdvertisement").HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.NumberOfAcceptOrder)
                   .HasColumnName("NumberOfAcceptOrder").HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.PaymentStatus)
                   .HasColumnName("PaymentStatus")
                   .HasConversion(new EnumToStringConverter<PaymentStatus>())
                   .HasColumnType("varchar")
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(x => x.PlanName)
                .HasColumnName("PlanName")
                .HasColumnType("varchar")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.StartDate)
                   .HasColumnName("StartDate")
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            builder.Property(x => x.EndDate)
                   .HasColumnName("EndDate")
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            builder.Property(x => x.ArchivedAt)
                   .HasColumnName("ArchivedAt")
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

        }
    }
}
