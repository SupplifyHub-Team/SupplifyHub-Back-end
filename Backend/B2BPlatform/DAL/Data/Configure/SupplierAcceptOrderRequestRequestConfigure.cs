using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class SupplierAcceptOrderRequestRequestConfigure : IEntityTypeConfiguration<SupplierAcceptOrderRequest>
    {
        public void Configure(EntityTypeBuilder<SupplierAcceptOrderRequest> builder)
        {
            // Set the table name
            builder.ToTable("SupplierAcceptOrderRequests");

            // Set the primary key
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("Id")
                   .ValueGeneratedOnAdd();

            // Configure properties
            builder.Property(x => x.SupplierId)
                   .HasColumnName("SupplierId")
                   .HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.RequestedAmount)
                   .HasColumnName("RequestedAmount")
                   .HasColumnType("int")
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasColumnName("Status")
                    .HasConversion(new EnumToStringConverter<RequestStatus>())
                    .HasColumnType("varchar")
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("CreatedAt")
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // Nullable properties
            builder.Property(x => x.ProcessedAt)
                   .HasColumnName("ProcessedAt")
                   .HasColumnType("timestamp with time zone")
                   .IsRequired(false);

            // Configure the relationship with the Supplier entity
            builder.HasOne(x => x.Supplier)
                   .WithMany(x => x.SupplierAcceptOrderRequests) // No navigation property on the Supplier side for requests
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade); // If a supplier is deleted, their requests are too
        }
    }

}
