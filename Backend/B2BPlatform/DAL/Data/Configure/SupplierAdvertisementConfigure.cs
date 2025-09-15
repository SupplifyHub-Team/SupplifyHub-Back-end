using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities;

public class SupplierAdvertisementConfigure : IEntityTypeConfiguration<SupplierAdvertisement>
{
    public void Configure(EntityTypeBuilder<SupplierAdvertisement> builder)
    {

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
        builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired(false);
        builder.Property(x => x.ImageUrl).HasColumnName("ImageUrl").HasColumnType("varchar").IsRequired();
        builder.Property(x => x.ImagePublicId).HasColumnName("ImagePublicId").HasColumnType("varchar").IsRequired();
        builder.Property(x => x.Title).HasColumnName("Title").HasColumnType("varchar").IsRequired();
        builder.Property(x => x.TargetUrl).HasColumnName("TargetUrl").HasColumnType("varchar").IsRequired(false);
        builder.Property(x => x.StartDate).HasColumnName("StartDate").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("EndDate").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").HasColumnType("boolean").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.impressions).HasColumnName("Impressions").HasColumnType("int").IsRequired(false);
        builder.Property(x => x.clicks).HasColumnName("Clicks").HasColumnType("int").IsRequired(false);
    }
}
