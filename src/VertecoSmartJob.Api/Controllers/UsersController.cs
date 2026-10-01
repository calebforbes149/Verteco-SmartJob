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
[Authorize(Roles = "Manager")]
[Route("api/v1/users")]
public class UsersController(IUserRepository users, UserManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserDto>>> List(
        [FromQuery] string? q = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string sort = "name", CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest(new ProblemDetails { Title = "Invalid pagination", Status = 400 });
        var items = await users.ListAsync(q, ct);
        var sorted = sort == "-name" ? items.OrderByDescending(u => u.FullName) : items.OrderBy(u => u.FullName);
        return Ok(new PagedResponse<UserDto>
        {
            Items = sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(u => u.ToDto()).ToList(),
            TotalCount = items.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)items.Count / pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await service.CreateAsync(new User
        {
            FullName = request.FullName.Trim(), Email = request.Email.Trim(), Phone = request.Phone.Trim(),
            Role = request.Role, TechnicianId = request.TechnicianId
        }, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user.ToDto());
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserDto>> Patch(Guid id, PatchUserRequest request, CancellationToken ct)
    {
        var user = await service.UpdateAsync(id, u =>
        {
            if (request.FullName is not null) u.FullName = request.FullName.Trim();
            if (request.Email is not null) u.Email = request.Email.Trim();
            if (request.Phone is not null) u.Phone = request.Phone.Trim();
        }, ct);
        return Ok(user.ToDto());
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<UserDto>> Suspend(Guid id, CancellationToken ct) => Ok((await service.SuspendAsync(id, ct)).ToDto());

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<UserDto>> Reactivate(Guid id, CancellationToken ct) => Ok((await service.ReactivateAsync(id, ct)).ToDto());

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
