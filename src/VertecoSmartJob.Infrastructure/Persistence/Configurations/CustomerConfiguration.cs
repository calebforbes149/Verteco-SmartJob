using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Industry).HasMaxLength(100);
        b.Property(x => x.ContactName).HasMaxLength(150);
        b.Property(x => x.ContactEmail).HasMaxLength(200);
        b.Property(x => x.ContactPhone).HasMaxLength(50);
        b.Property(x => x.RowVersion).IsConcurrencyToken();

        b.HasMany(x => x.Sites)
            .WithOne(s => s.Customer)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.SiteType).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsConcurrencyToken();

        b.HasMany(x => x.Equipment)
            .WithOne(e => e.Site)
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.JobCards)
            .WithOne(j => j.Site)
            .HasForeignKey(j => j.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.SerialNumber).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.SerialNumber).IsUnique();
    }
}
