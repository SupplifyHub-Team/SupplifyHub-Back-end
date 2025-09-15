//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Entities;
//namespace DAL.Data.Configure
//{
//    public class PasswordResetTokenConfigure : IEntityTypeConfiguration<PasswordResetToken>
//    {
//        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
//        {
//            builder.ToTable("PasswordResetTokens");
//            builder.HasKey(x => x.Id);
//            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
//            builder.Property(x => x.UserId).HasColumnName("UserId").HasColumnType("int").IsRequired(true);
//            builder.Property(x => x.TokenHash).HasColumnName("TokenHash").HasColumnType("varchar").HasMaxLength(255).IsRequired(true);
//            builder.Property(x => x.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("timestamp with time zone").IsRequired(true);
//            builder.Property(x => x.IsUsed).HasColumnName("IsUsed").HasColumnType("boolean").HasDefaultValue(false).IsRequired(true);
//            builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
//            builder.HasOne(t => t.User)
//                   .WithMany(u => u.passwordResetTokens)
//                   .HasForeignKey(t => t.UserId)
//                   .OnDelete(DeleteBehavior.Cascade); // Deleting a user will also delete their tokens.
//        }
//    }

//}
