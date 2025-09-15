using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities; // مكان الكلاس Blog

namespace DAL.Data.Configure
{
    public class BlogConfigure : IEntityTypeConfiguration<Blog>
    {
        public void Configure(EntityTypeBuilder<Blog> builder)
        {
            builder.ToTable("Blogs"); // Explicitly set table name

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                   .HasColumnName("Id")
                   .HasColumnType("int")
                   .ValueGeneratedOnAdd();

            builder.Property(b => b.Title)
                   .HasColumnName("Title")
                   .HasColumnType("varchar")
                   .HasMaxLength(2550)
                   .IsRequired();

            builder.Property(b => b.Content)
                   .HasColumnName("Content")
                   .HasColumnType("text")
                   .IsRequired();

            builder.Property(b => b.Excerpt)
                   .HasColumnName("Excerpt")
                   .HasColumnType("varchar")
                   .HasMaxLength(5000);

            builder.Property(b => b.CoverImageUrl)
                   .HasColumnName("CoverImageUrl")
                   .HasColumnType("varchar");

            builder.Property(b => b.PublicImageId)
                   .HasColumnName("PublicImageId")
                   .HasColumnType("varchar");

            builder.Property(b => b.PdfUrl)
                   .HasColumnName("PdfUrl")
                   .HasColumnType("varchar")
                   .IsRequired(false);

            builder.Property(b => b.PublicPdfId)
                   .HasColumnName("PublicPdfId")
                   .HasColumnType("varchar")
                   .IsRequired(false);


            builder.Property(b => b.CreatedAt)
                   .HasColumnName("CreatedAt")
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(b => b.UpdatedAt)
                   .HasColumnName("UpdatedAt")
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
