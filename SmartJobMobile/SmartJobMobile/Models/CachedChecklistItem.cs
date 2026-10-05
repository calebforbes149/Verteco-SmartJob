using SQLite;

namespace SmartJobMobile.Models;

public class CachedChecklistItem
{
    // Combined key because checklist item IDs
    // are unique server GUIDs.
    [PrimaryKey]
    public string ChecklistItemId { get; set; } =
        string.Empty;

    [Indexed]
    public string JobId { get; set; } =
        string.Empty;

    public string Text { get; set; } =
        string.Empty;

    public bool IsDone { get; set; }

    // RowVersion of this specific checklist item.
    // Required by Person 4's sync push API.
    public string RowVersion { get; set; } =
        string.Empty;

    public DateTime LastUpdated { get; set; }
}