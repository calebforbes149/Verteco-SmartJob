using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

// Sets up the database rules for mobile sync operations
public class MobileSyncOperationConfiguration
    : IEntityTypeConfiguration<MobileSyncOperation>
{
    public void Configure(EntityTypeBuilder<MobileSyncOperation> b)
    {
        b.Property(x => x.OperationType)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.Result)
            .IsRequired()
            .HasMaxLength(1000);

        // Stops the same mobile operation from being processed twice
        b.HasIndex(x => new
        {
            x.TechnicianId,
            x.OperationId
        }).IsUnique();

        b.HasIndex(x => x.JobCardId);

        b.HasIndex(x => x.ProcessedAtUtc);
    }
}