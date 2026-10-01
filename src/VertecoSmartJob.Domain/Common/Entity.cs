namespace VertecoSmartJob.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>EF Core concurrency token. Mobile sync uses this to detect a job card that
    /// changed on the server since the offline queue captured it, per the sync design.</summary>
    public byte[] RowVersion { get; set; } = [];
}

public abstract class SoftDeletableEntity : Entity
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
    }
}
