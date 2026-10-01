using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobcards")]
[Route("api/v1/jobs")]
public class JobCardsController(JobCardService jobCardService) : ControllerBase
{
    private Guid ActorTechnicianId => Guid.TryParse(User.FindFirstValue("TechnicianId"), out var id) ? id : Guid.Empty;
    /// <summary>Paged, filterable, sortable list of job cards — the one endpoint the
    /// Part 2 "vertical slice" was built around first.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobCardSummaryDto>>> Search(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] string? status = null, [FromQuery] string? priority = null,
        [FromQuery] bool? flagged = null,
        [FromQuery] Guid? customerId = null, [FromQuery] Guid? siteId = null, [FromQuery] Guid? technicianId = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 ||
            (status is not null && !Enum.TryParse<JobStatus>(status, true, out _)) ||
            (priority is not null && !Enum.TryParse<JobPriority>(priority, true, out _)))
            return BadRequest(new ProblemDetails { Title = "Invalid job query", Status = 400 });
        var query = new JobCardQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Q = q,
            Status = Enum.TryParse<JobStatus>(status, true, out var s) ? s : null,
            Priority = Enum.TryParse<JobPriority>(priority, true, out var p) ? p : null,
            CustomerId = customerId,
            SiteId = siteId,
            TechnicianId = User.IsInRole("Technician") ? ActorTechnicianId : technicianId,
            Flagged = flagged
        };

        var result = await jobCardService.SearchAsync(query, ct);
        return Ok(new PagedResponse<JobCardSummaryDto>
        {
            Items = result.Items.Select(j => j.ToSummaryDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobCardDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var job = await jobCardService.GetByIdAsync(id, ct);
        if (job is not null && User.IsInRole("Technician") && job.TechnicianId != ActorTechnicianId) return Forbid();
        return job is null ? NotFound() : Ok(job.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<JobCardDetailDto>> Create(CreateJobCardRequest request, CancellationToken ct)
    {
        var priority = Enum.Parse<JobPriority>(request.Priority, true);
        var job = await jobCardService.CreateAsync(request.CustomerId, request.SiteId, priority,
            request.ScheduledDate, request.Notes, request.TechnicianId, ct);

        var full = await jobCardService.GetByIdAsync(job.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, full!.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<JobCardDetailDto>> Assign(Guid id, AssignTechnicianRequest request, CancellationToken ct)
    {
        await jobCardService.AssignAsync(id, request.TechnicianId, ct);
        var job = await jobCardService.GetByIdAsync(id, ct);
        return Ok(job!.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<JobCardDetailDto>> Patch(Guid id, PatchJobCardRequest request, CancellationToken ct)
    {
        var priority = request.Priority is null ? (JobPriority?)null : Enum.Parse<JobPriority>(request.Priority, true);
        await jobCardService.UpdateDraftAsync(id, priority, request.ScheduledDate, request.Notes, ct);
        return Ok((await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/complete")]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<JobCardDetailDto>> Complete(Guid id, CompleteJobRequest request, CancellationToken ct)
    {
        await jobCardService.CompleteAsync(id, request.ReviewNotes, ct);
        var job = await jobCardService.GetByIdAsync(id, ct);
        return Ok(job!.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<JobCardDetailDto>> Reopen(Guid id, ReopenJobRequest request, CancellationToken ct)
    {
        await jobCardService.ReopenAsync(id, request.Reason, ct);
        var job = await jobCardService.GetByIdAsync(id, ct);
        return Ok(job!.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<JobCardDetailDto>> Cancel(Guid id, CancelJobRequest request, CancellationToken ct)
    {
        await jobCardService.CancelAsync(id, request.Reason, ct);
        var job = await jobCardService.GetByIdAsync(id, ct);
        return Ok(job!.ToDetailDto());
    }

    [HttpPost("{id:guid}/flag")]
    public async Task<ActionResult<JobCardDetailDto>> Flag(Guid id, FlagJobRequest request, CancellationToken ct)
    {
        if (!User.IsInRole("Manager") && !User.IsInRole("Technician")) return Forbid();
        var existing = await jobCardService.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();
        if (User.IsInRole("Technician") && existing.TechnicianId != ActorTechnicianId) return Forbid();
        var job = await jobCardService.FlagAsync(id, request.Reason, ct);
        return Ok(job.ToDetailDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/resolve-flag")]
    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<JobCardDetailDto>> ResolveFlag(Guid id, ResolveFlagRequest request, CancellationToken ct)
    {
        var job = await jobCardService.ResolveFlagAsync(id, request.ResolutionNotes, ct: ct);
        return Ok(job.ToDetailDto());
    }
}
