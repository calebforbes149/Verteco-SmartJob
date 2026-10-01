using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/corrective-actions")]
public sealed class CorrectiveActionsController(JobCardService jobs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CorrectiveActionDto>>> List(
        [FromQuery] string? status = null, [FromQuery] Guid? jobId = null, [FromQuery] string? q = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string sort = "-date",
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 ||
            (status is not null && !status.Equals("open", StringComparison.OrdinalIgnoreCase) &&
             !status.Equals("resolved", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new ProblemDetails { Title = "Invalid corrective-action query", Status = 400 });

        var result = await jobs.SearchCorrectiveActionsAsync(new CorrectiveActionQuery
        {
            Page = page, PageSize = pageSize, JobCardId = jobId, Q = q, Sort = sort,
            IsOpen = status is null ? null : status.Equals("open", StringComparison.OrdinalIgnoreCase)
        }, ct);
        return Ok(new PagedResponse<CorrectiveActionDto>
        {
            Items = result.Items.Select(a => a.ToDto()).ToList(),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
            TotalPages = result.TotalPages
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CorrectiveActionDto>> Get(Guid id, CancellationToken ct)
    {
        var action = await jobs.GetCorrectiveActionByIdAsync(id, ct);
        return action is null ? NotFound() : Ok(action.ToDto());
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<CorrectiveActionDto>> Resolve(Guid id, ResolveFlagRequest request, CancellationToken ct)
    {
        await jobs.ResolveCorrectiveActionAsync(id, request.ResolutionNotes, ct);
        return Ok((await jobs.GetCorrectiveActionByIdAsync(id, ct))!.ToDto());
    }
}
