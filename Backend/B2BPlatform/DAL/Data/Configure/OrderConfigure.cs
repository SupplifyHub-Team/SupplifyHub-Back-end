using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    // Orders Configurations
    public class OrderConfigure : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            //builder.Property(x => x.DescriptionAndQuantity).HasColumnName("DescriptionAndQuantity").HasColumnType("text").IsRequired(false);
            builder.Property(x => x.ContactPersonName).HasColumnName("ContactPersonName").HasColumnType("varchar").HasMaxLength(200).IsRequired(true);
            builder.Property(x => x.ContactPersonNumber).HasColumnName("ContactPersonNumber").HasColumnType("varchar").HasMaxLength(20).IsRequired(true);
            builder.Property(x => x.RequiredLocation).HasColumnName("RequiredLocation").HasColumnType("text").HasMaxLength(255).IsRequired(false);
            //builder.Property(x => x.Quantity).HasColumnName("Quantity").HasColumnType("int").IsRequired();
            builder.Property(x => x.NumSuppliersDesired).HasColumnName("NumSuppliersDesired").HasColumnType("int").IsRequired();
            builder.Property(x => x.OrderStatus).HasColumnName("OrderStatus").HasConversion(new EnumToStringConverter<OrderStatus>()).HasColumnType("varchar").HasDefaultValue(OrderStatus.Active).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Deadline).HasColumnName("Deadline").HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");


            // Foreign Keys
            builder.Property(x => x.UserId).HasColumnName("UserId").HasColumnType("int").IsRequired();
            builder.Property(x => x.CategoryId).HasColumnName("CategoryId").HasColumnType("int").IsRequired();

            // Relationships
            builder.HasOne(o => o.User)
                   .WithMany(c => c.Orders)
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting company if orders are associated

            builder.HasOne(o => o.Category)
                   .WithMany(cat => cat.Orders)
                   .HasForeignKey(o => o.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if orders are associated
            
            // One-to-Many with OrderItems
            builder.HasMany(o => o.Items)
                   .WithOne(i => i.Order)
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
