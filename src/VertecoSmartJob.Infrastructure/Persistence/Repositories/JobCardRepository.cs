using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Common;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Repositories;

public class JobCardRepository(AppDbContext db) : IJobCardRepository
{
    private IQueryable<JobCard> FullGraph() => db.JobCards.AsSplitQuery()
        .Include(j => j.Customer)
        .Include(j => j.Site).ThenInclude(s => s!.Equipment)
        .Include(j => j.JobTemplate)
        .Include(j => j.Technician)
        .Include(j => j.ChecklistItems)
        .Include(j => j.Revisions)
        .Include(j => j.Evidence)
        .Include(j => j.Signatures)
        .Include(j => j.CorrectiveActions);

    public async Task<JobCard?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await FullGraph().FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<PagedResult<JobCard>> SearchAsync(JobCardQuery query, CancellationToken ct = default)
    {
        var jobs = FullGraph().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim().ToLower();
            jobs = jobs.Where(j =>
                j.Number.ToLower().Contains(q) ||
                j.Customer!.Name.ToLower().Contains(q) ||
                j.Site!.Name.ToLower().Contains(q) ||
                j.Site.Equipment.Any(e => e.SerialNumber.ToLower().Contains(q)) ||
                (j.Technician != null && j.Technician.FullName.ToLower().Contains(q)));
        }

        if (query.Status is { } status) jobs = jobs.Where(j => j.Status == status);
        if (query.Priority is { } priority) jobs = jobs.Where(j => j.Priority == priority);
        if (query.CustomerId is { } customerId) jobs = jobs.Where(j => j.CustomerId == customerId);
        if (query.SiteId is { } siteId) jobs = jobs.Where(j => j.SiteId == siteId);
        if (query.TechnicianId is { } technicianId) jobs = jobs.Where(j => j.TechnicianId == technicianId);
        if (query.Flagged is { } flagged) jobs = jobs.Where(j => j.IsFlagged == flagged);

        jobs = ApplySort(jobs, query.Sort);

        var total = await jobs.CountAsync(ct);
        var page = Math.Max(query.Page, 1);
        var pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 20;

        var items = await jobs.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<JobCard> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<CorrectiveAction>> SearchCorrectiveActionsAsync(CorrectiveActionQuery query, CancellationToken ct = default)
    {
        var actions = db.CorrectiveActions.AsNoTracking().Include(a => a.JobCard).AsQueryable();
        if (query.IsOpen is { } isOpen)
            actions = isOpen ? actions.Where(a => a.ResolvedAtUtc == null) : actions.Where(a => a.ResolvedAtUtc != null);
        if (query.JobCardId is { } jobId) actions = actions.Where(a => a.JobCardId == jobId);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var text = query.Q.Trim().ToLower();
            actions = actions.Where(a => a.Reason.ToLower().Contains(text) || a.JobCard!.Number.ToLower().Contains(text));
        }
        actions = query.Sort == "date" ? actions.OrderBy(a => a.OpenedAtUtc) : actions.OrderByDescending(a => a.OpenedAtUtc);
        var total = await actions.CountAsync(ct);
        var items = await actions.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<CorrectiveAction>
        {
            Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize
        };
    }

    public Task<CorrectiveAction?> GetCorrectiveActionByIdAsync(Guid id, CancellationToken ct = default) =>
        db.CorrectiveActions.AsNoTracking().Include(a => a.JobCard).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<JobCard>> GetDashboardSnapshotAsync(CancellationToken ct = default) =>
        await FullGraph().OrderByDescending(j => j.ScheduledDate).ToListAsync(ct);

    public Task<List<JobCard>> GetForReportingAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        db.JobCards.AsNoTracking()
            .Where(j => j.CreatedAtUtc >= fromUtc && j.CreatedAtUtc < toUtc ||
                        j.CompletedAtUtc >= fromUtc && j.CompletedAtUtc < toUtc)
            .ToListAsync(ct);

    public async Task AddAsync(JobCard jobCard, CancellationToken ct = default) =>
        await db.JobCards.AddAsync(jobCard, ct);

    public async Task AddRevisionAsync(JobCardRevision revision, CancellationToken ct = default) =>
        await db.JobCardRevisions.AddAsync(revision, ct);

    public async Task AddEvidenceAsync(JobEvidence evidence, CancellationToken ct = default) =>
        await db.JobEvidence.AddAsync(evidence, ct);

    public async Task AddSignatureAsync(JobSignature signature, CancellationToken ct = default) =>
        await db.JobSignatures.AddAsync(signature, ct);

    public void RemoveEvidence(JobEvidence evidence) => db.JobEvidence.Remove(evidence);

    public void RemoveSignature(JobSignature signature) => db.JobSignatures.Remove(signature);

    public async Task AddCorrectiveActionAsync(CorrectiveAction action, CancellationToken ct = default) =>
        await db.CorrectiveActions.AddAsync(action, ct);

    public Task<string> NextJobNumberAsync(CancellationToken ct = default) =>
        Task.FromResult($"JC-{DateTime.UtcNow.Year}-{Guid.NewGuid():N}"[..24].ToUpperInvariant());

    private static IQueryable<JobCard> ApplySort(IQueryable<JobCard> jobs, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var field = sort?.TrimStart('-').ToLowerInvariant();

        return field switch
        {
            "number" => descending ? jobs.OrderByDescending(j => j.Number) : jobs.OrderBy(j => j.Number),
            "priority" => descending ? jobs.OrderByDescending(j => j.Priority) : jobs.OrderBy(j => j.Priority),
            "status" => descending ? jobs.OrderByDescending(j => j.Status) : jobs.OrderBy(j => j.Status),
            "scheduleddate" => descending ? jobs.OrderByDescending(j => j.ScheduledDate) : jobs.OrderBy(j => j.ScheduledDate),
            _ => jobs.OrderByDescending(j => j.ScheduledDate)
        };
    }
}
