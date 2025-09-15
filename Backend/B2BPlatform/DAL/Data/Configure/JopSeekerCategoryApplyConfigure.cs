using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    public class JopSeekerCategoryApplyConfigure : IEntityTypeConfiguration<JopSeekerCategoryApply>
    {
        public void Configure(EntityTypeBuilder<JopSeekerCategoryApply> builder)
        {
            builder.ToTable("JopSeekerCategoryApplies");
            builder.HasKey(x => new { x.JopSeekerId, x.CategoryId }); // Composite primary key

            // Foreign Keys
            builder.Property(x => x.JopSeekerId).HasColumnName("JopSeekerId").HasColumnType("int").IsRequired();
            builder.Property(x => x.CategoryId).HasColumnName("CategoryId").HasColumnType("int").IsRequired();

            // Foreign Key relationships are defined in IndividualConfigure and CategoryConfigure
        }
    }

}
