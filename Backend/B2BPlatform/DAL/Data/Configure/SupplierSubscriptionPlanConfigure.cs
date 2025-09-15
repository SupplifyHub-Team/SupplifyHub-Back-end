using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
using Enum;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Data.Configure
{
    public class SupplierSubscriptionPlanConfigure : IEntityTypeConfiguration<SupplierSubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SupplierSubscriptionPlan> builder)
        {
            builder.ToTable("SupplierSubscriptionPlans");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.SupplierId, x.PlanId, x.StartDate })
                   .IsUnique();
            builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired();
            builder.Property(x => x.PlanId).HasColumnName("PlanId").HasColumnType("int").IsRequired();    
            builder.Property(x => x.ProductVisitsAndPerformanceAnalysis).HasColumnName("ProductVisitsAndPerformanceAnalysis").HasColumnType("boolean").HasDefaultValue(false).IsRequired();    
            builder.Property(x => x.CompetitorAndMarketAnalysis).HasColumnName("CompetitorAndMarketAnalysis").HasColumnType("boolean").HasDefaultValue(false).IsRequired();    
            builder.Property(x => x.DirectTechnicalSupport).HasColumnName("DirectTechnicalSupport").HasColumnType("boolean").HasDefaultValue(false).IsRequired();    
            builder.Property(x => x.EarlyAccessToOrder).HasColumnName("EarlyAccessToOrder").HasColumnType("boolean").HasDefaultValue(false).IsRequired();    
            builder.Property(x => x.ShowHigherInSearch).HasColumnName("ShowHigherInSearch").HasColumnType("boolean").HasDefaultValue(false).IsRequired();    
            builder.Property(x => x.NumberOfAcceptOrder).HasColumnName("NumberOfAcceptOrder").HasColumnType("int").HasDefaultValue(30).IsRequired();    
            builder.Property(x => x.NumberOfAdvertisement).HasColumnName("NumberOfAdvertisement").HasColumnType("int").HasDefaultValue(0).IsRequired();    
            builder.Property(x => x.NumberOfProduct).HasColumnName("NumberOfProduct").HasColumnType("int").HasDefaultValue(0).IsRequired();    
            builder.Property(x => x.NumberOfSpecialProduct).HasColumnName("NumberOfSpecialProduct").HasColumnType("int").HasDefaultValue(0).IsRequired();    
            
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
            builder.Property(x => x.PaymentStatus).HasColumnName("PaymentStatus").HasConversion(new EnumToStringConverter<PaymentStatus>()).HasColumnType("varchar").HasDefaultValue(PaymentStatus.Pending).HasMaxLength(15).IsRequired();
            builder.Property(x => x.PlanName).HasColumnName("PlanName").HasColumnType("varchar").HasMaxLength(100).IsRequired();
            builder.Property(x => x.StartDate).HasColumnName("StartDate").HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.EndDate).HasColumnName("EndDate").HasColumnType("timestamp with time zone").IsRequired();
        }
    }

}
