using VertecoSmartJob.Application.Common;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Abstractions;

public interface IJobCardRepository
{
    Task<JobCard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<JobCard>> SearchAsync(JobCardQuery query, CancellationToken ct = default);
    Task<PagedResult<CorrectiveAction>> SearchCorrectiveActionsAsync(CorrectiveActionQuery query, CancellationToken ct = default);
    Task<CorrectiveAction?> GetCorrectiveActionByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(JobCard jobCard, CancellationToken ct = default);
    Task AddRevisionAsync(JobCardRevision revision, CancellationToken ct = default);
    Task AddEvidenceAsync(JobEvidence evidence, CancellationToken ct = default);
    Task AddSignatureAsync(JobSignature signature, CancellationToken ct = default);
    void RemoveEvidence(JobEvidence evidence);
    void RemoveSignature(JobSignature signature);
    Task AddCorrectiveActionAsync(CorrectiveAction action, CancellationToken ct = default);
    Task<string> NextJobNumberAsync(CancellationToken ct = default);
    Task<List<JobCard>> GetDashboardSnapshotAsync(CancellationToken ct = default);
    Task<List<JobCard>> GetForReportingAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
