using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

public sealed class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> b)
    {
        b.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        b.Property(x => x.ResolutionNotes).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsConcurrencyToken();
        b.HasOne(x => x.JobCard).WithMany(j => j.CorrectiveActions)
            .HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.JobCardId, x.ResolvedAtUtc });
        b.HasIndex(x => x.OpenedAtUtc);
    }
}
