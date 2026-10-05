using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Infrastructure.Identity;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/account/profile")]
public sealed class AccountProfileController(
    ICurrentUser currentUser,
    IUserRepository users,
    ITechnicianRepository technicians,
    IAuditLogRepository audit,
    IUnitOfWork unitOfWork,
    PasswordHashingService hasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Unauthorized();

        var user = await users.GetByIdAsync(userId, ct);
        return user is null ? NotFound() : Ok(user.ToDto());
    }

    [HttpPatch]
    public async Task<ActionResult<UserDto>> Update(
        UpdateOwnProfileRequest request,
        CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Unauthorized();

        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();

        var name = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;

        if (name.Length is < 1 or > 150 ||
            email.Length is < 3 or > 200 ||
            phone.Length > 50 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Enter a valid name, email address and phone number."
            });
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                !hasher.Verify(user, request.CurrentPassword))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Enter your current password to change your email address."
                });
            }

            var existingUser = await users.GetByEmailAsync(email, ct);
            var existingTechnician = await technicians.GetByEmailAsync(email, ct);
            if (existingUser is not null && existingUser.Id != user.Id ||
                existingTechnician is not null && existingTechnician.Id != user.TechnicianId)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "That email address is already in use."
                });
            }
        }

        user.FullName = name;
        user.Email = email;
        user.Phone = phone;

        if (user.TechnicianId is { } technicianId)
        {
            var technician = await technicians.GetByIdAsync(technicianId, ct);
            if (technician is not null)
            {
                technician.FullName = name;
                technician.Email = email;
                technician.Phone = phone;
            }
        }

        await audit.AddAsync(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "OwnProfileUpdated",
            Details = user.Id.ToString()
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(user.ToDto());
    }
}
