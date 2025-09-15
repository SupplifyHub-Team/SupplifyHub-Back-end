using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Configure
{
    public class UserRequestCategoryConfigure : IEntityTypeConfiguration<UserRequestCategory>
    {
        public void Configure(EntityTypeBuilder<UserRequestCategory> builder)
        {
            builder.ToTable("UserRequestCategories"); // Explicitly set table name if different from class name
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("int").ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("Name").HasColumnType("varchar").HasMaxLength(255).IsRequired();
            builder.Property(x => x.UserId).HasColumnName("UserId").HasColumnType("int").IsRequired();
        }
    }
}
