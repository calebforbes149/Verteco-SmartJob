using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Common;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Equipment> Equipment => Set<Equipment>();

    public DbSet<Technician> Technicians => Set<Technician>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<JobTemplate> JobTemplates =>
        Set<JobTemplate>();

    public DbSet<TemplateChecklistItem>
        TemplateChecklistItems =>
            Set<TemplateChecklistItem>();

    public DbSet<JobCard> JobCards => Set<JobCard>();

    public DbSet<JobChecklistItem>
        JobChecklistItems =>
            Set<JobChecklistItem>();

    public DbSet<JobCardRevision>
        JobCardRevisions =>
            Set<JobCardRevision>();

    public DbSet<JobEvidence> JobEvidence =>
        Set<JobEvidence>();

    public DbSet<JobSignature> JobSignatures =>
        Set<JobSignature>();

    public DbSet<CorrectiveAction>
        CorrectiveActions =>
            Set<CorrectiveAction>();

    public DbSet<MobileSyncOperation>
        MobileSyncOperations =>
            Set<MobileSyncOperation>();

    public DbSet<MobileSyncChange>
        MobileSyncChanges =>
            Set<MobileSyncChange>();

    public DbSet<PasswordResetToken>
        PasswordResetTokens =>
            Set<PasswordResetToken>();

    public DbSet<RefreshToken>
        RefreshTokens =>
            Set<RefreshToken>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        // Global soft-delete filter:
        // retention means anonymise/hide,
        // not hard-delete job data.
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Site>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Technician>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<User>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<JobTemplate>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<JobCard>()
            .HasQueryFilter(e => !e.IsDeleted);
    }

    async Task IUnitOfWork.SaveChangesAsync(
        CancellationToken ct)
    {
        await SaveChangesAsync(ct);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken ct = default)
    {
        // Find jobs that changed before adding
        // sync records to the tracker.
        var changedJobIds =
            ChangeTracker.Entries()
                .Where(entry =>
                    entry.State is
                        EntityState.Added or
                        EntityState.Modified &&
                    (entry.Entity is JobCard ||
                     entry.Entity is JobChecklistItem))
                .Select(entry =>
                    entry.Entity switch
                    {
                        JobCard job =>
                            job.Id,

                        JobChecklistItem item =>
                            item.JobCardId,

                        _ => Guid.Empty
                    })
                .Where(id =>
                    id != Guid.Empty)
                .Distinct()
                .ToList();

        foreach (var jobId in changedJobIds)
        {
            MobileSyncChanges.Add(
                new MobileSyncChange
                {
                    JobCardId = jobId,
                    ChangedAtUtc =
                        DateTime.UtcNow
                });
        }

        // Refresh concurrency tokens for
        // added or modified entities.
        foreach (
            var entry in
            ChangeTracker.Entries<Entity>())
        {
            if (entry.State is
                EntityState.Added or
                EntityState.Modified)
            {
                entry.Entity.RowVersion =
                    Guid.NewGuid()
                        .ToByteArray();
            }
        }

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            ct);
    }
}