using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Application.Management;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/technicians")]
public class TechniciansController(
    ITechnicianRepository technicians, IUserRepository users, TechnicianManagementService service) : ControllerBase
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
        var accountIds = (await users.ListAsync(ct: ct))
            .Where(u => u.TechnicianId.HasValue)
            .GroupBy(u => u.TechnicianId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);
        return Ok(new PagedResponse<TechnicianDto>
        {
            Items = ordered.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => t.ToDto(accountIds.TryGetValue(t.Id, out var userId) ? userId : null)).ToList(),
            TotalCount = items.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)items.Count / pageSize)
        });
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TechnicianDto>> GetById(Guid id, CancellationToken ct)
    {
        var technician = await technicians.GetByIdAsync(id, ct);
        if (technician is null) return NotFound();
        var account = await users.GetByTechnicianIdAsync(id, ct);
        return Ok(technician.ToDto(account?.Id));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<TechnicianDto>> Create(CreateTechnicianRequest request, CancellationToken ct)
    {
        var technician = new Technician
        {
            FullName = request.FullName.Trim(), Email = request.Email.Trim(), Phone = request.Phone.Trim(),
            Skills = request.Skills.Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Availability = Enum.Parse<TechnicianAvailability>(request.Availability, true)
        };
        var created = await service.CreateAsync(technician, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Technician.Id },
            created.Technician.ToDto(created.Account.Id));
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TechnicianDto>> Patch(Guid id, PatchTechnicianRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, technician =>
        {
            if (request.FullName is not null) technician.FullName = request.FullName.Trim();
            if (request.Email is not null) technician.Email = request.Email.Trim();
            if (request.Phone is not null) technician.Phone = request.Phone.Trim();
            if (request.Skills is not null)
                technician.Skills = request.Skills.Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (request.Availability is not null)
                technician.Availability = Enum.Parse<TechnicianAvailability>(request.Availability, true);
        }, ct);
        return Ok(updated.Technician.ToDto(updated.Account?.Id));
    }
}
