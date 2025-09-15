using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(r => r.Rating).HasColumnName("Rating").IsRequired();
            builder.Property(r => r.Comment).HasColumnName("Comment").HasMaxLength(2000);
            builder.Property(r => r.SubmittedAt).HasColumnName("SubmittedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(r => r.RevieweeId).HasColumnName("RevieweeId").HasColumnType("int").IsRequired();
            builder.Property(r => r.ReviewerId).HasColumnName("ReviewerId").HasColumnType("int").IsRequired();
            builder.Property(r => r.DealId).HasColumnName("DealId").HasColumnType("int").IsRequired();

            builder.HasIndex(r => new { r.DealId, r.ReviewerId }).IsUnique();
            
            builder.HasOne(r => r.Deal)
                   .WithMany(d => d.Reviews)
                   .HasForeignKey(r => r.DealId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Reviewer)
                   .WithMany(u => u.ReviewsGiven)
                   .HasForeignKey(r => r.ReviewerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Reviewee)
                   .WithMany(u => u.ReviewsReceived)
                   .HasForeignKey(r => r.RevieweeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
