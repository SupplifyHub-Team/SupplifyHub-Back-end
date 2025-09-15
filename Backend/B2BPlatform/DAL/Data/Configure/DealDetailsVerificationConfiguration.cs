using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    public class DealDetailsVerificationConfiguration : IEntityTypeConfiguration<DealDetailsVerification>
    {
        public void Configure(EntityTypeBuilder<DealDetailsVerification> builder)
        {
            builder.ToTable("DealDetailsVerification");
            builder.HasKey(dd => dd.Id);
            builder.Property(dd => dd.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            //builder.Property(dd => dd.Quantity).HasColumnName("Quantity").IsRequired();
            //builder.Property(dd => dd.DiscriptionAndQuantity).HasColumnName("DiscriptionAndQuantity").HasColumnType("text").IsRequired();
            //builder.Property(dd => dd.Price).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(dd => dd.SubmittedAt).HasColumnName("SubmittedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(dd => dd.DateOfDelivered).HasColumnType("timestamp with time zone").HasColumnName("DateOfDelivered").IsRequired();
            builder.Property(dd => dd.DealId).HasColumnName("DealId").HasColumnType("int").IsRequired();
            builder.Property(dd => dd.SubmittedById).HasColumnName("SubmittedById").HasColumnType("int").IsRequired();
            builder.Property(d => d.DealDoneAt).HasColumnName("DealDoneAt").HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp with time zone");
            builder.HasIndex(dd => new { dd.DealId, dd.SubmittedById }).IsUnique();
            builder.HasOne(dd => dd.Deal)
                   .WithMany(d => d.DealDetailsVerifications)
                   .HasForeignKey(dd => dd.DealId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(dd => dd.User)
                   .WithMany(u => u.DealDetailsVerifications)
                   .HasForeignKey(dd => dd.SubmittedById)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(dd => dd.Items)
                   .WithOne(i => i.DealDetailsVerification)
                   .HasForeignKey(i => i.DealDetailsVerificationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
