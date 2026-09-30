using FluentValidation;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Api.Validation;

public class CreateJobCardRequestValidator : AbstractValidator<CreateJobCardRequest>
{
    public CreateJobCardRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Priority).Must(p => Enum.TryParse<JobPriority>(p, true, out _))
            .WithMessage("Priority must be one of: Low, Medium, High, Urgent.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class AssignTechnicianRequestValidator : AbstractValidator<AssignTechnicianRequest>
{
    public AssignTechnicianRequestValidator()
    {
        RuleFor(x => x.TechnicianId).NotEmpty();
    }
}

public class PatchJobCardRequestValidator : AbstractValidator<PatchJobCardRequest>
{
    public PatchJobCardRequestValidator()
    {
        RuleFor(x => x.Priority).Must(p => Enum.TryParse<JobPriority>(p, true, out _))
            .When(x => x.Priority is not null);
        RuleFor(x => x.ScheduledDate).NotEmpty().When(x => x.ScheduledDate is not null);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CompleteJobRequestValidator : AbstractValidator<CompleteJobRequest>
{
    public CompleteJobRequestValidator()
    {
        RuleFor(x => x.ReviewNotes).MaximumLength(2000);
    }
}

public class ReopenJobRequestValidator : AbstractValidator<ReopenJobRequest>
{
    public ReopenJobRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to reopen a job card.").MaximumLength(1000);
    }
}

public class CancelJobRequestValidator : AbstractValidator<CancelJobRequest>
{
    public CancelJobRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}

