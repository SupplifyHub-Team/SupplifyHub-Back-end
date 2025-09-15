using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    // Job & Category Configurations
    public class JobPostConfigure : IEntityTypeConfiguration<JobPost>
    {
        public void Configure(EntityTypeBuilder<JobPost> builder)
        {
            builder.ToTable("JobPost");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.PostedAt).HasColumnName("PostedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(x => x.HrEmail).HasColumnName("HrEmail").HasColumnType("varchar").HasMaxLength(255).IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasColumnType("boolean").HasDefaultValue(true);

            // Foreign Keys
            builder.Property(x => x.SupplierId).HasColumnName("SupplierId").HasColumnType("int").IsRequired();
            builder.Property(x => x.CategoryId).HasColumnName("CategoryId").HasColumnType("int").IsRequired();

            // Relationships
            builder.HasOne(j => j.Supplier)
                   .WithMany(c => c.JobPosts)
                   .HasForeignKey(j => j.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting company if jobs are associated

            builder.HasOne(j => j.Category)
                   .WithMany(cat => cat.JobPosts)
                   .HasForeignKey(j => j.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if jobs are associated
        }
    }

}
