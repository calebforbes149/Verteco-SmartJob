namespace VertecoSmartJob.Web.Api;

public class AuditEntryDto
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? JobCardId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}