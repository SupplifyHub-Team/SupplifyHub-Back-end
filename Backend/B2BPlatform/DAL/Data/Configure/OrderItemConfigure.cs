using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities;

namespace DAL.Data.Configure
{
    // OrderItems Configurations
    public class OrderItemConfigure : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();

            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(300).IsRequired();

            builder.Property(x => x.Quantity).HasColumnName("Quantity").HasColumnType("int").IsRequired();

            builder.Property(x => x.Notes).HasColumnName("Notes").HasColumnType("text").IsRequired(false);

            // Foreign Key
            builder.Property(x => x.OrderId).HasColumnName("OrderId").HasColumnType("int").IsRequired();

        }
    }
}
