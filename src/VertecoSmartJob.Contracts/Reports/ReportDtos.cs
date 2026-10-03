namespace VertecoSmartJob.Contracts.Reports;

public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int JobsCreated { get; set; }
    public int JobsCompleted { get; set; }
    public double? AverageCompletionHours { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = [];
    public string? AiCommentary { get; set; }
    public string AiStatus { get; set; } = "Unavailable";
}
