namespace SmartJobMobile.Models;

public class Job
{
    // Real JobCard ID from Isaac's API
    public Guid ApiId { get; set; }

    // Kept for compatibility with the existing MAUI UI
    public int Id { get; set; }

    public string JobNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTime ScheduledDate { get; set; }

    public string ScheduledDateDisplay =>
        ScheduledDate.ToString("dd MMM yyyy • HH:mm");
}