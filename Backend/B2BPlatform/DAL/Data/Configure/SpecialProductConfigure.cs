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
    internal class SpecialProductConfigure: IEntityTypeConfiguration<SpecialProduct>
    {
        public void Configure(EntityTypeBuilder<SpecialProduct> builder)
        {
            builder.ToTable("SpecialProducts"); // Explicitly set table name if different from class name
            builder.HasKey(x => x.ProductId);
            builder.Property(x => x.ProductId).HasColumnName("ProductId").HasColumnType("int");
            builder.Property(x => x.Offer).HasColumnName("Offer").HasColumnType("int").IsRequired(true);


        }
    }
}
