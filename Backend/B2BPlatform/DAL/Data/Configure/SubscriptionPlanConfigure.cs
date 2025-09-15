using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    // Subscription Plans Configurations
    public class SubscriptionPlanConfigure : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.ToTable("SubscriptionPlans");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Pros).HasColumnName("Pros").HasColumnType("TEXT[]").IsRequired(false);
            builder.Property(x => x.Cons).HasColumnName("Cons").HasColumnType("TEXT[]").IsRequired(false);
            builder.Property(x => x.Price).HasColumnName("Price").HasColumnType("decimal(18,3)").IsRequired();
            builder.Property(x => x.Description).HasColumnName("Description").HasColumnType("text").IsRequired(false);
            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP"); // Or update on save
            builder.Property(x => x.Duration).HasColumnName("Duration").HasColumnType("int").IsRequired();

            // Relationships
            builder.HasMany(sp => sp.SupplierSubscriptionPlans)
                   .WithOne(csp => csp.SubscriptionPlan)
                   .HasForeignKey(csp => csp.PlanId)
                   .OnDelete(DeleteBehavior.Restrict);
           
            builder.HasMany(sp => sp.TempForSupplierSubscriptionPlans)
                   .WithOne(csp => csp.SubscriptionPlan)
                   .HasForeignKey(csp => csp.PlanId)
                   .OnDelete(DeleteBehavior.Restrict);// Prevent deleting a plan if companies are subscribed to it
        }
    }

}
