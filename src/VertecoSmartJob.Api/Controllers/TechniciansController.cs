using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/technicians")]
public class TechniciansController(ITechnicianRepository technicians) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TechnicianDto>>> List(
        [FromQuery] string? q = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string sort = "name", CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
            return BadRequest(new ProblemDetails { Title = "Invalid pagination", Status = 400 });
        var items = await technicians.ListAsync(ct);
        if (!string.IsNullOrWhiteSpace(q))
            items = items.Where(t => t.FullName.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var ordered = sort == "-name" ? items.OrderByDescending(t => t.FullName) : items.OrderBy(t => t.FullName);
        return Ok(new PagedResponse<TechnicianDto>
        {
            Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(t => t.ToDto()).ToList(),
            TotalCount = items.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)items.Count / pageSize)
        });
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TechnicianDto>> GetById(Guid id, CancellationToken ct)
    {
        var technician = await technicians.GetByIdAsync(id, ct);
        return technician is null ? NotFound() : Ok(technician.ToDto());
    }
}
