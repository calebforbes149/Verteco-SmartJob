using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> b)
    {
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.Email).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.RowVersion).IsConcurrencyToken();

        var comparer = new ValueComparer<List<string>>(
            (a, c) => (a ?? new()).SequenceEqual(c ?? new()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        b.Property(x => x.Skills)
            .HasConversion(
                v => string.Join('|', v),
                v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(comparer);
    }
}
