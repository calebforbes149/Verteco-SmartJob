using SQLite;

namespace SmartJobMobile.Models;

public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Unique key so the same operation is not
    // accidentally processed twice.
    [Indexed]
    public string IdempotencyKey { get; set; } =
        Guid.NewGuid().ToString();

    public string JobId { get; set; } = string.Empty;

    // Examples:
    // StartJob
    // UpdateChecklist
    // UpdateEquipment
    // AddComments
    // SubmitJob
    // UploadEvidence
    public string OperationType { get; set; } = string.Empty;

    // JSON representation of the data that
    // eventually needs to be sent to the API.
    public string PayloadJson { get; set; } = string.Empty;

    // RowVersion the job had when the
    // offline operation was created.
    public string BaseRowVersion { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public int RetryCount { get; set; }

    public string Status { get; set; } = "Pending";

    public string LastError { get; set; } = string.Empty;
}