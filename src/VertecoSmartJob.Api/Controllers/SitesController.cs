using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Management;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sites")]
public class SitesController(ISiteRepository sites, SiteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SiteDto>>> List(
        [FromQuery] Guid? customerId = null, [FromQuery] string? q = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "name", CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest(new ProblemDetails { Title = "Invalid pagination", Status = 400 });
        var items = customerId is { } id ? await sites.ListByCustomerAsync(id, ct) : await sites.ListAsync(q, ct);
        if (!string.IsNullOrWhiteSpace(q)) items = items.Where(s => s.Name.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var sorted = sort == "-name" ? items.OrderByDescending(s => s.Name) : items.OrderBy(s => s.Name);
        return Ok(new PagedResponse<SiteDto>
        {
            Items = sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(s => s.ToDto()).ToList(),
            TotalCount = items.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)items.Count / pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SiteDto>> GetById(Guid id, CancellationToken ct)
    {
        var site = await sites.GetByIdAsync(id, ct);
        return site is null ? NotFound() : Ok(site.ToDto());
    }

    /// <summary>Warranty/history lookup by equipment serial number — Verteco keeps job card
    /// data indefinitely so a technician can trace an asset's full service record.</summary>
    [HttpGet("equipment/by-serial/{serialNumber}")]
    public async Task<ActionResult<EquipmentDto>> FindBySerial(string serialNumber, CancellationToken ct)
    {
        var equipment = await sites.FindBySerialNumberAsync(serialNumber, ct);
        return equipment is null ? NotFound() : Ok(new EquipmentDto { Id = equipment.Id, Name = equipment.Name, SerialNumber = equipment.SerialNumber, Description = equipment.Description });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<SiteDto>> Create(CreateSiteRequest request, CancellationToken ct)
    {
        var site = await service.CreateAsync(new Site
        {
            CustomerId = request.CustomerId, Name = request.Name.Trim(),
            Address = request.Address.Trim(), SiteType = request.SiteType.Trim()
        }, ct);
        return CreatedAtAction(nameof(GetById), new { id = site.Id }, site.ToDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SiteDto>> Patch(Guid id, PatchSiteRequest request, CancellationToken ct)
    {
        var site = await service.UpdateAsync(id, s =>
        {
            if (request.Name is not null) s.Name = request.Name.Trim();
            if (request.Address is not null) s.Address = request.Address.Trim();
            if (request.SiteType is not null) s.SiteType = request.SiteType.Trim();
        }, ct);
        return Ok(site.ToDto());
    }
}
