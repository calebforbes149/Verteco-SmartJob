using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Common;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/audit")]
public sealed class AuditController(IAuditLogRepository audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditEntryDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? jobId = null, [FromQuery] string? q = null,
        [FromQuery] string sort = "-date", CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest(new ProblemDetails { Title = "Invalid pagination", Status = 400 });
        var entries = await audit.ListAsync(ct);
        if (jobId is { } id) entries = entries.Where(e => e.JobCardId == id).ToList();
        if (!string.IsNullOrWhiteSpace(q)) entries = entries.Where(e => e.Action.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var ordered = sort == "date" ? entries.OrderBy(e => e.CreatedAtUtc) : entries.OrderByDescending(e => e.CreatedAtUtc);
        return Ok(new PagedResponse<AuditEntryDto>
        {
            Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(e => new AuditEntryDto
            {
                Id = e.Id, ActorUserId = e.ActorUserId, JobCardId = e.JobCardId,
                Action = e.Action, Details = e.Details, CreatedAtUtc = e.CreatedAtUtc
            }).ToList(),
            TotalCount = entries.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)entries.Count / pageSize)
        });
    }
}

public sealed class AuditEntryDto
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? JobCardId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
