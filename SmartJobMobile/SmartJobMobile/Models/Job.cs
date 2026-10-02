namespace SmartJobMobile.Models;

public class Job
{
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