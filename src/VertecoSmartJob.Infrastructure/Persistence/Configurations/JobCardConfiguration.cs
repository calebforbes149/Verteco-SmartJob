using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

public class JobCardConfiguration : IEntityTypeConfiguration<JobCard>
{
    public void Configure(EntityTypeBuilder<JobCard> b)
    {
        b.Property(x => x.Number).IsRequired().HasMaxLength(30);
        b.HasIndex(x => x.Number).IsUnique();

        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.ReviewNotes).HasMaxLength(2000);
        b.Property(x => x.ReopenReason).HasMaxLength(1000);
        b.Property(x => x.CancelReason).HasMaxLength(1000);
        b.Property(x => x.SealedHash).HasMaxLength(64);
        b.Property(x => x.RowVersion).IsConcurrencyToken();

        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Site).WithMany(s => s.JobCards).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.JobTemplate).WithMany().HasForeignKey(x => x.JobTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Technician).WithMany(t => t.JobCards).HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.ChecklistItems)
            .WithOne(i => i.JobCard)
            .HasForeignKey(i => i.JobCardId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ScheduledDate);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.SiteId);
        b.HasIndex(x => x.TechnicianId);
    }
}

public class JobCardRevisionConfiguration : IEntityTypeConfiguration<JobCardRevision>
{
    public void Configure(EntityTypeBuilder<JobCardRevision> b)
    {
        b.Property(x => x.ContentJson).IsRequired();
        b.Property(x => x.ContentHash).IsRequired().HasMaxLength(64);
        b.HasIndex(x => new { x.JobCardId, x.Number }).IsUnique();
        b.HasOne(x => x.JobCard).WithMany(j => j.Revisions).HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobEvidenceConfiguration : IEntityTypeConfiguration<JobEvidence>
{
    public void Configure(EntityTypeBuilder<JobEvidence> b)
    {
        b.Property(x => x.Reference).IsRequired().HasMaxLength(500);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.HasOne(x => x.JobCard).WithMany(j => j.Evidence).HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobSignatureConfiguration : IEntityTypeConfiguration<JobSignature>
{
    public void Configure(EntityTypeBuilder<JobSignature> b)
    {
        b.Property(x => x.Type).IsRequired().HasMaxLength(20);
        b.Property(x => x.SignerName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Reference).IsRequired().HasMaxLength(500);
        b.HasOne(x => x.JobCard).WithMany(j => j.Signatures).HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobChecklistItemConfiguration : IEntityTypeConfiguration<JobChecklistItem>
{
    public void Configure(EntityTypeBuilder<JobChecklistItem> b)
    {
        b.Property(x => x.Text).IsRequired().HasMaxLength(300);
    }
}

