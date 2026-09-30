namespace VertecoSmartJob.Domain.Exceptions;

/// <summary>
/// Base for every rule violation raised by the Domain layer. The API's global exception
/// handler maps subclasses to HTTP status codes instead of leaking stack traces.
/// </summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>Job card asked to move from a status that cannot reach the requested status. Maps to 409.</summary>
public sealed class InvalidJobStatusTransitionException(string fromStatus, string attemptedAction)
    : DomainException($"Job card in status '{fromStatus}' cannot perform '{attemptedAction}'.")
{
    public string FromStatus { get; } = fromStatus;
    public string AttemptedAction { get; } = attemptedAction;
}

/// <summary>Technician tried to submit a job for review with outstanding checklist items. Maps to 422.</summary>
public sealed class ChecklistNotCompleteException(int remainingItems)
    : DomainException($"{remainingItems} checklist item(s) are still outstanding.")
{
    public int RemainingItems { get; } = remainingItems;
}

/// <summary>
/// The job card's checklist/notes changed after it was sealed for review, so the manager's
/// approval would be signing off on content they never saw. Maps to 409.
/// </summary>
public sealed class JobCardModifiedSinceReviewException()
    : DomainException("This job card changed after it was submitted for review. Ask the technician to resubmit.");

/// <summary>A manager action (e.g. Reopen) requires a written reason and none was supplied. Maps to 422.</summary>
public sealed class ReasonRequiredException(string action)
    : DomainException($"A reason is required to {action} this job card.");

public sealed class SubmissionInvalidException(string message) : DomainException(message);

/// <summary>Flag or corrective-action operation is incompatible with its current state. Maps to 409.</summary>
public sealed class InvalidFlagStateException(string message) : DomainException(message);
