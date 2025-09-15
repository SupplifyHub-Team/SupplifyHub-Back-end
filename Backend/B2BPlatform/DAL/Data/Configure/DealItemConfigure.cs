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
    public class DealItemConfigure: IEntityTypeConfiguration<DealItem>
    {
        
    
        public void Configure(EntityTypeBuilder<DealItem> builder)
        {
            builder.ToTable("DealItems");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                   .HasColumnName("Id")
                   .ValueGeneratedOnAdd();

            builder.Property(i => i.Name)
                   .HasColumnName("Name")
                   .HasColumnType("varchar(255)")
                   .IsRequired();

            builder.Property(i => i.Quantity)
                   .HasColumnName("Quantity")
                   .IsRequired();

            builder.Property(i => i.Price)
                   .HasColumnName("Price")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(i => i.DealDetailsVerificationId)
                   .HasColumnName("DealDetailsVerificationId")
                   .IsRequired();
        }
    } 
}
