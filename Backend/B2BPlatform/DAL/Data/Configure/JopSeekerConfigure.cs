using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    // Individual User Configurations
    public class JopSeekerConfigure : IEntityTypeConfiguration<JopSeeker>
    {
        public void Configure(EntityTypeBuilder<JopSeeker> builder)
        {
            builder.ToTable("JopSeekers");
            builder.HasKey(x => x.UserId); // UserId is also the PK here for one-to-one
            builder.Property(x => x.UserId).HasColumnName("UserId").HasColumnType("int"); // No ValueGeneratedOnAdd for FK PK
            builder.Property(x => x.ResumePath).HasColumnName("ResumePath").HasColumnType("varchar").HasMaxLength(1000).IsRequired();

            // Foreign Key relationship to User is defined in UserConfigure
            builder.HasMany(i => i.JopSeekerCategoryApplies)
                   .WithOne(ica => ica.Individual)
                   .HasForeignKey(ica => ica.JopSeekerId)
                   .OnDelete(DeleteBehavior.Cascade); // If individual is deleted, their applications are deleted
        }
    }

}
