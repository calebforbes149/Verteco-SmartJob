using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Jobs;

public interface ISubmissionValidationStrategy
{
    string? Validate(JobCard job);
}

public sealed class RequiredFieldsStrategy : ISubmissionValidationStrategy
{
    public string? Validate(JobCard job) =>
        job.CustomerId == Guid.Empty || job.SiteId == Guid.Empty || job.TechnicianId is null ||
        job.ScheduledDate == default || string.IsNullOrWhiteSpace(job.Number) ||
        job.ChecklistItems.Count == 0 || job.ChecklistItems.Any(i => !i.IsDone)
            ? "Required job details and checklist responses must be complete." : null;
}

public sealed class EvidencePresenceStrategy : ISubmissionValidationStrategy
{
    public string? Validate(JobCard job) => job.Evidence.Count == 0
        ? "At least one evidence item is required." : null;
}

public sealed class SignaturePresenceStrategy : ISubmissionValidationStrategy
{
    public string? Validate(JobCard job) =>
        job.Signatures.Any(s => s.Type == "Technician") && job.Signatures.Any(s => s.Type == "Client")
            ? null : "Technician and client signatures are required.";
}
