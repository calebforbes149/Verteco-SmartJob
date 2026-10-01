using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

// A shared account record; technician-specific work details remain in Technician.
public class User : SoftDeletableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Technician";
    public Guid? TechnicianId { get; set; }
    public Technician? Technician { get; set; }
    public bool IsSuspended { get; private set; }

    public void Suspend() => IsSuspended = true;
    public void Reactivate() => IsSuspended = false;
}

public class AuditLog : Entity
{
    public Guid? ActorUserId { get; set; }
    public Guid? JobCardId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
