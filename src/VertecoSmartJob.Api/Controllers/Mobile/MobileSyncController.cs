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

    // Pulls the technician's latest jobs from the server
    [HttpGet("pull")]
    public async Task<ActionResult<MobileSyncPullResponse>> Pull(
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        var result = await syncService.PullAsync(
            ActorTechnicianId,
            ct);

        return Ok(new MobileSyncPullResponse
        {
            ServerTimeUtc = result.ServerTimeUtc,

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
                    IsDone = operation.IsDone
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
                        Message = operation.Message
                    })
                .ToList()
        });
    }
}