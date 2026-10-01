using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Api.Controllers.Mobile;

/// <summary>
/// Mobile-facing job card API used by field technicians.
/// All operations are restricted to the authenticated technician's
/// assigned job cards.
/// </summary>
[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/v1/mobile/jobs")]
public class MobileJobsController(JobCardService jobCardService) : ControllerBase
{
    private Guid ActorTechnicianId =>
        Guid.TryParse(User.FindFirstValue("TechnicianId"), out var id)
            ? id
            : Guid.Empty;

    /// <summary>
    /// Returns job cards assigned to the authenticated technician.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobCardSummaryDto>>> GetMyJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        if (page < 1 || pageSize is < 1 or > 200)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid paging values",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await jobCardService.SearchAsync(
            new JobCardQuery
            {
                Page = page,
                PageSize = pageSize,
                TechnicianId = ActorTechnicianId,
                Sort = "scheduleddate"
            },
            ct);

        return Ok(new PagedResponse<JobCardSummaryDto>
        {
            Items = result.Items.Select(j => j.ToSummaryDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Returns the full mobile view of one assigned job.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobCardDetailDto>> GetJob(
        Guid id,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        var job = await jobCardService.GetByIdAsync(id, ct);

        if (job is null)
            return NotFound();

        if (job.TechnicianId != ActorTechnicianId)
            return Forbid();

        return Ok(job.ToDetailDto());
    }

    /// <summary>
    /// Technician accepts an assigned job.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<JobCardDetailDto>> AcceptJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.AcceptAsync(id, ActorTechnicianId, ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    /// <summary>
    /// Technician starts an accepted or reopened job.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<JobCardDetailDto>> StartJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.StartAsync(id, ActorTechnicianId, ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    /// <summary>
    /// Updates the completion state of a checklist item.
    /// The current domain model implements this as a toggle.
    /// </summary>
    [HttpPatch("{id:guid}/checklist/{checklistItemId:guid}/toggle")]
    public async Task<ActionResult<JobCardDetailDto>> ToggleChecklistItem(
        Guid id,
        Guid checklistItemId,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.ToggleChecklistItemAsync(
            id,
            checklistItemId,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    /// <summary>
    /// Submits the completed field job for management review.
    /// Existing domain validation checks checklist completion,
    /// signatures/evidence rules and creates the sealed revision.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<JobCardDetailDto>> SubmitJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.SubmitForReviewAsync(
            id,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    private async Task<bool> CanAccessJob(
        Guid jobId,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return false;

        var job = await jobCardService.GetByIdAsync(jobId, ct);

        return job is not null &&
               job.TechnicianId == ActorTechnicianId;
    }
}