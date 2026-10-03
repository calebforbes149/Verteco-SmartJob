using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

// Sets up the database rules for mobile sync changes
public class MobileSyncChangeConfiguration
    : IEntityTypeConfiguration<MobileSyncChange>
{
    public void Configure(EntityTypeBuilder<MobileSyncChange> b)
    {
        b.HasIndex(x => x.JobCardId);

        b.HasIndex(x => x.ChangedAtUtc);

        // Helps delta pulls find changes for a job efficiently
        b.HasIndex(x => new
        {
            x.JobCardId,
            x.ChangedAtUtc
        });
    }
}