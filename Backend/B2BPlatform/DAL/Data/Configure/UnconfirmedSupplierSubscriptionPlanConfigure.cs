using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configure
{
    public class UnconfirmedSupplierSubscriptionPlanConfigure: IEntityTypeConfiguration<UnconfirmedSupplierSubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<UnconfirmedSupplierSubscriptionPlan> builder)
        {
            builder.ToTable("UnconfirmedSupplierSubscriptionPlans");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired();
            builder.Property(x => x.PlanId).HasColumnName("PlanId").HasColumnType("int").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        }
    }
}
