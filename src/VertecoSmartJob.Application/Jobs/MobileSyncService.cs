using System.Text.Json;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Jobs;

// Handles pull and push operations for the mobile offline queue
public class MobileSyncService(
    IJobCardRepository jobCards,
    IMobileSyncRepository syncRepository,
    IAuditLogRepository audit,
    IUnitOfWork unitOfWork)
{
    // Returns the technician's current jobs to the mobile app
    public async Task<MobileSyncPullResult> PullAsync(
        Guid technicianId,
        CancellationToken ct = default)
    {
        var jobs = await syncRepository.GetJobsForTechnicianAsync(
            technicianId,
            ct);

        return new MobileSyncPullResult
        {
            ServerTimeUtc = DateTime.UtcNow,

            Jobs = jobs.Select(job => new MobileSyncJob
            {
                Id = job.Id,
                Number = job.Number,
                Status = job.Status.ToString(),
                RowVersion = Convert.ToBase64String(job.RowVersion),
                ScheduledDate = job.ScheduledDate,
                Notes = job.Notes,

                ChecklistItems = job.ChecklistItems
                    .OrderBy(item => item.Order)
                    .Select(item => new MobileSyncChecklistItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        IsDone = item.IsDone,
                        RowVersion = Convert.ToBase64String(
                            item.RowVersion)
                    })
                    .ToList()
            }).ToList()
        };
    }

    // Processes operations that were queued while the phone was offline
    public async Task<MobileSyncPushResult> PushAsync(
        Guid technicianId,
        IEnumerable<MobileSyncOperationInput> operations,
        CancellationToken ct = default)
    {
        var response = new MobileSyncPushResult();

        foreach (var operation in operations)
        {
            var result = await ProcessOperationAsync(
                technicianId,
                operation,
                ct);

            response.Results.Add(result);
        }

        return response;
    }

    private async Task<MobileSyncOperationOutput> ProcessOperationAsync(
        Guid technicianId,
        MobileSyncOperationInput request,
        CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty)
        {
            return CreateResult(
                request,
                "Rejected",
                null,
                "OperationId is required.");
        }

        if (request.JobCardId == Guid.Empty)
        {
            return CreateResult(
                request,
                "Rejected",
                null,
                "JobCardId is required.");
        }

        // Returns the old result if the phone retries the same operation
        var previousOperation =
            await syncRepository.GetOperationAsync(
                technicianId,
                request.OperationId,
                ct);

        if (previousOperation is not null)
        {
            var previousResult =
                JsonSerializer.Deserialize<MobileSyncOperationOutput>(
                    previousOperation.Result);

            if (previousResult is not null)
                return previousResult;

            return CreateResult(
                request,
                "AlreadyProcessed",
                null,
                "This operation has already been processed.");
        }

        var job = await jobCards.GetByIdAsync(
            request.JobCardId,
            ct);

        if (job is null)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    null,
                    "Job card was not found."),
                ct);
        }

        // A technician can only sync jobs assigned to them
        if (job.TechnicianId != technicianId)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "The job is not assigned to this technician."),
                ct);
        }

        if (!TryReadRowVersion(
                request.RowVersion,
                out var mobileJobRowVersion))
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "A valid RowVersion is required."),
                ct);
        }

        var jobChanged =
            !mobileJobRowVersion.SequenceEqual(job.RowVersion);

        switch (request.OperationType.Trim().ToLowerInvariant())
        {
            case "setchecklistitem":
                return await ApplyChecklistOperationAsync(
                    technicianId,
                    job,
                    request,
                    jobChanged,
                    ct);

            default:
                if (jobChanged)
                {
                    return await SaveResultAsync(
                        technicianId,
                        request,
                        CreateResult(
                            request,
                            "Conflict",
                            GetRowVersion(job),
                            "The job changed on the server while the device was offline."),
                        ct);
                }

                return await SaveResultAsync(
                    technicianId,
                    request,
                    CreateResult(
                        request,
                        "Rejected",
                        GetRowVersion(job),
                        $"Unsupported operation type '{request.OperationType}'."),
                    ct);
        }
    }

    private async Task<MobileSyncOperationOutput>
        ApplyChecklistOperationAsync(
            Guid technicianId,
            JobCard job,
            MobileSyncOperationInput request,
            bool jobChanged,
            CancellationToken ct)
    {
        if (request.ChecklistItemId is null ||
            request.ChecklistItemId == Guid.Empty)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "ChecklistItemId is required."),
                ct);
        }

        if (request.IsDone is null)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "IsDone is required."),
                ct);
        }

        var checklistItem = job.ChecklistItems
            .FirstOrDefault(
                item => item.Id == request.ChecklistItemId.Value);

        if (checklistItem is null)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "Checklist item was not found."),
                ct);
        }

        if (!TryReadRowVersion(
                request.ChecklistItemRowVersion ?? string.Empty,
                out var mobileItemRowVersion))
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Rejected",
                    GetRowVersion(job),
                    "A valid ChecklistItemRowVersion is required."),
                ct);
        }

        var checklistItemChanged =
            !mobileItemRowVersion.SequenceEqual(
                checklistItem.RowVersion);

        // The same checklist field changed on both sides
        if (checklistItemChanged &&
            checklistItem.IsDone != request.IsDone.Value)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Conflict",
                    GetRowVersion(job),
                    "This checklist item changed on the server while the device was offline."),
                ct);
        }

        // The server already has the value requested by the phone
        if (checklistItemChanged &&
            checklistItem.IsDone == request.IsDone.Value)
        {
            return await SaveResultAsync(
                technicianId,
                request,
                CreateResult(
                    request,
                    "Applied",
                    GetRowVersion(job),
                    "Checklist item already has the requested value."),
                ct);
        }

        // A stale job is safe when this specific item did not change
        if (jobChanged && !checklistItemChanged)
        {
            job.SetChecklistItem(
                request.ChecklistItemId.Value,
                request.IsDone.Value,
                technicianId);
        }
        else
        {
            job.SetChecklistItem(
                request.ChecklistItemId.Value,
                request.IsDone.Value,
                technicianId);
        }

        // Marks the parent job as changed for mobile conflict detection
        job.RowVersion = Guid.NewGuid().ToByteArray();

        var temporaryResult = CreateResult(
            request,
            "Applied",
            GetRowVersion(job),
            "Checklist item synced.");

        var operation = new MobileSyncOperation
        {
            TechnicianId = technicianId,
            JobCardId = job.Id,
            OperationId = request.OperationId,
            OperationType = request.OperationType.Trim(),
            Result = JsonSerializer.Serialize(temporaryResult),
            ProcessedAtUtc = DateTime.UtcNow
        };

        await syncRepository.AddOperationAsync(
            operation,
            ct);

        await audit.AddAsync(
            new AuditLog
            {
                JobCardId = job.Id,
                Action = "MobileChecklistSynced",
                Details = jobChanged
                    ? "Checklist item auto-merged during mobile sync."
                    : job.Status.ToString()
            },
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        // Save the final RowVersion so retries return the same result
        var finalResult = CreateResult(
            request,
            "Applied",
            GetRowVersion(job),
            jobChanged
                ? "Checklist item auto-merged."
                : "Checklist item synced.");

        await syncRepository.UpdateOperationResultAsync(
            request.OperationId,
            technicianId,
            JsonSerializer.Serialize(finalResult),
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return finalResult;
    }

    private async Task<MobileSyncOperationOutput> SaveResultAsync(
        Guid technicianId,
        MobileSyncOperationInput request,
        MobileSyncOperationOutput result,
        CancellationToken ct)
    {
        var operation = new MobileSyncOperation
        {
            TechnicianId = technicianId,
            JobCardId = request.JobCardId,
            OperationId = request.OperationId,
            OperationType = request.OperationType.Trim(),
            Result = JsonSerializer.Serialize(result),
            ProcessedAtUtc = DateTime.UtcNow
        };

        await syncRepository.AddOperationAsync(
            operation,
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return result;
    }

    private static MobileSyncOperationOutput CreateResult(
        MobileSyncOperationInput request,
        string status,
        string? rowVersion,
        string? message)
    {
        return new MobileSyncOperationOutput
        {
            OperationId = request.OperationId,
            JobCardId = request.JobCardId,
            Status = status,
            RowVersion = rowVersion,
            Message = message
        };
    }

    private static string GetRowVersion(JobCard job)
    {
        return Convert.ToBase64String(job.RowVersion);
    }

    private static bool TryReadRowVersion(
        string value,
        out byte[] rowVersion)
    {
        rowVersion = [];

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}