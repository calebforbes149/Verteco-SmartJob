using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Api.Controllers.Mobile;

// Handles offline sync requests from the mobile app
[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/v1/mobile/sync")]
public class MobileSyncController(
    MobileSyncService syncService) : ControllerBase
{
    private Guid ActorTechnicianId =>
        Guid.TryParse(User.FindFirstValue("TechnicianId"), out var id)
            ? id
            : Guid.Empty;

    // Pulls jobs changed since the mobile app's last successful sync
    [HttpGet("pull")]
    public async Task<ActionResult<MobileSyncPullResponse>> Pull(
        [FromQuery] DateTime? since,
        [FromQuery] List<Guid>? knownJobIds,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        if (knownJobIds is { Count: > 500 })
            return BadRequest(new ProblemDetails { Title = "At most 500 known job IDs are allowed", Status = 400 });

        var result = await syncService.PullAsync(
            ActorTechnicianId,
            since,
            knownJobIds,
            ct);

        return Ok(new MobileSyncPullResponse
        {
            ServerTimeUtc = result.ServerTimeUtc,
            RemovedJobIds = result.RemovedJobIds,

            Jobs = result.Jobs.Select(job =>
                new MobileSyncJobDto
                {
                    Id = job.Id,
                    Number = job.Number,
                    Status = job.Status,
                    RowVersion = job.RowVersion,
                    ScheduledDate = job.ScheduledDate,
                    Notes = job.Notes,

                    ChecklistItems = job.ChecklistItems
                        .Select(item =>
                            new MobileSyncChecklistItemDto
                            {
                                Id = item.Id,
                                Text = item.Text,
                                IsDone = item.IsDone,
                                RowVersion = item.RowVersion
                            })
                        .ToList()
                })
                .ToList()
        });
    }

    // Pushes operations that were queued while the phone was offline
    [HttpPost("push")]
    public async Task<ActionResult<MobileSyncPushResponse>> Push(
        [FromBody] MobileSyncPushRequest request,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        if (request.Operations is null || request.Operations.Count > 100 ||
            request.Operations.Any(operation => operation is null))
            return BadRequest(new ProblemDetails { Title = "Provide at most 100 valid operations per push", Status = 400 });

        var operations = request.Operations
            .Select(operation =>
                new MobileSyncOperationInput
                {
                    OperationId = operation.OperationId,
                    JobCardId = operation.JobCardId,
                    OperationType = operation.OperationType,
                    RowVersion = operation.RowVersion,
                    ChecklistItemId = operation.ChecklistItemId,
                    ChecklistItemRowVersion =
                        operation.ChecklistItemRowVersion,
                    IsDone = operation.IsDone,
                    EquipmentName = operation.EquipmentName,
                    SerialNumber = operation.SerialNumber,
                    EquipmentDescription = operation.EquipmentDescription
                })
            .ToList();

        var result = await syncService.PushAsync(
            ActorTechnicianId,
            operations,
            ct);

        return Ok(new MobileSyncPushResponse
        {
            Results = result.Results
                .Select(operation =>
                    new MobileSyncOperationResult
                    {
                        OperationId = operation.OperationId,
                        JobCardId = operation.JobCardId,
                        Status = operation.Status,
                        RowVersion = operation.RowVersion,
                        Message = operation.Message,
                        EquipmentId = operation.EquipmentId
                    })
                .ToList()
        });
    }
}
