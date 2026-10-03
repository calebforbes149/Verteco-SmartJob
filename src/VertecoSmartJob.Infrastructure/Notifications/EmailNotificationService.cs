using VertecoSmartJob.Application.Abstractions;

namespace VertecoSmartJob.Infrastructure.Notifications;

public class EmailNotificationService(IUserRepository users, IEmailSender emailSender) : INotificationService
{
    public async Task NotifyJobAssignedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default)
    {
        var technicianUser = await users.GetByTechnicianIdAsync(technicianId, ct);
        if (technicianUser is null) return;

        await emailSender.SendAsync(
            technicianUser.Email,
            $"New job assigned: {jobNumber}",
            $"<p>Hi {technicianUser.FullName},</p><p>You've been assigned job <strong>{jobNumber}</strong>. Log in to SmartJob to view the details.</p>",
            ct);
    }

    public async Task NotifyJobSubmittedAsync(string jobNumber, CancellationToken ct = default)
    {
        foreach (var manager in await GetManagersAsync(ct))
        {
            await emailSender.SendAsync(
                manager.Email,
                $"Job submitted for review: {jobNumber}",
                $"<p>Hi {manager.FullName},</p><p>Job <strong>{jobNumber}</strong> has been submitted and is awaiting your approval.</p>",
                ct);
        }
    }

    public async Task NotifyJobApprovedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default)
    {
        var technicianUser = await users.GetByTechnicianIdAsync(technicianId, ct);
        if (technicianUser is null) return;

        await emailSender.SendAsync(
            technicianUser.Email,
            $"Job approved: {jobNumber}",
            $"<p>Hi {technicianUser.FullName},</p><p>Job <strong>{jobNumber}</strong> has been reviewed and approved. Nice work.</p>",
            ct);
    }

    public async Task NotifyJobReopenedAsync(Guid technicianId, string jobNumber, string reason, CancellationToken ct = default)
    {
        var technicianUser = await users.GetByTechnicianIdAsync(technicianId, ct);
        if (technicianUser is null) return;

        await emailSender.SendAsync(
            technicianUser.Email,
            $"Job reopened: {jobNumber}",
            $"<p>Hi {technicianUser.FullName},</p><p>Job <strong>{jobNumber}</strong> has been reopened by a manager.</p><p>Reason: {reason}</p>",
            ct);
    }

    public async Task NotifyJobFlaggedAsync(string jobNumber, string reason, CancellationToken ct = default)
    {
        foreach (var manager in await GetManagersAsync(ct))
        {
            await emailSender.SendAsync(
                manager.Email,
                $"Job flagged: {jobNumber}",
                $"<p>Hi {manager.FullName},</p><p>Job <strong>{jobNumber}</strong> has been flagged.</p><p>Reason: {reason}</p>",
                ct);
        }
    }

    private async Task<IEnumerable<Domain.Entities.User>> GetManagersAsync(CancellationToken ct) =>
        (await users.ListAsync(ct: ct)).Where(u => u.Role == "Manager" && !u.IsSuspended);
}