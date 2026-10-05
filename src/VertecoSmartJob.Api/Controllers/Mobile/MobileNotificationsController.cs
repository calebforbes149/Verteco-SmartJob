using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Notifications;

namespace VertecoSmartJob.Api.Controllers.Mobile;

[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/v1/mobile/notifications")]
public class MobileNotificationsController(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<NotificationDto>>> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId)
            return Forbid();

        var result = await notifications.ListForUserAsync(userId, page, pageSize, ct);

        return Ok(new PagedResponse<NotificationDto>
        {
            Items = result.Items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Forbid();

        var notification = await notifications.GetByIdAsync(id, ct);
        if (notification is null || notification.UserId != userId)
            return NotFound();

        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }
}