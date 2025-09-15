using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities;
namespace DAL.Data.Configure
{
    public class UserTokenConfigure : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("UserTokens");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id");
            builder.Property(x => x.IsRevoked).HasColumnName("IsRevoked").HasColumnType("boolean").HasDefaultValue(false);
            builder.Property(x => x.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("timestamp with time zone").IsRequired(true);
            builder.Property(x => x.Token).HasColumnName("Token").HasColumnType("varchar").IsRequired(true);

            // Foreign Key is defined in UserConfigure
        }
    }

}
