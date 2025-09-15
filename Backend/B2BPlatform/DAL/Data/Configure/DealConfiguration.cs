using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class DealConfiguration : IEntityTypeConfiguration<Deal>
    {
        public void Configure(EntityTypeBuilder<Deal> builder)
        {
            builder.ToTable("Deals");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(d => d.Status).HasColumnName("Status").HasConversion(new EnumToStringConverter<DealStatus>())
                .HasColumnType("varchar").HasMaxLength(50).HasDefaultValue(DealStatus.Pending).IsRequired();
            builder.Property(d => d.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired();
            builder.Property(d => d.ClientId).HasColumnName("ClientId").HasColumnType("int").IsRequired();
            builder.Property(d => d.OrderId).HasColumnName("OrderId").HasColumnType("int").IsRequired();

            builder.HasOne(d => d.Order)
                   .WithOne(o => o.Deal)
                   .HasForeignKey<Deal>(d => d.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Client)
                   .WithMany(u => u.Deals) // Assuming a User can be a client in many deals
                   .HasForeignKey(d => d.ClientId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Supplier)
                   .WithMany(u => u.Deals) // Assuming a User can be a supplier in many deals
                   .HasForeignKey(d => d.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
