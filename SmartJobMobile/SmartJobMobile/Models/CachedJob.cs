using SQLite;

namespace SmartJobMobile.Models;

public class CachedJob
{
    [PrimaryKey]
    public string JobId { get; set; } = string.Empty;

    public string JobNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTime ScheduledDate { get; set; }

    public string Description { get; set; } = string.Empty;

    // Version received from the API.
    // Used later when detecting sync conflicts.
    public string RowVersion { get; set; } = string.Empty;

    // When this local copy was last updated.
    public DateTime LastUpdated { get; set; }

    // True when the technician has changed
    // this job locally and it still needs syncing.
    public bool HasLocalChanges { get; set; }
}