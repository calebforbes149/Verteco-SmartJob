namespace VertecoSmartJob.Contracts.Reports;

public sealed record ReportAnalysisSection(string Heading, string Body);

/// <summary>
/// Splits plain-text AI commentary into display sections without treating the
/// model's response as HTML or Markdown.
/// </summary>
public static class ReportAnalysisFormatter
{
    private static readonly string[] KnownHeadings =
    [
        "Monthly overview",
        "Workflow bottlenecks",
        "Risk and priority",
        "Recommended actions",
        "Data limitations",
        "Delivery time",
        "Attention points",
        "Suggested follow-up",
        "How to read this report"
    ];

    public static IReadOnlyList<ReportAnalysisSection> Parse(string? commentary)
    {
        if (string.IsNullOrWhiteSpace(commentary)) return [];

        var sections = new List<ReportAnalysisSection>();
        var body = new List<string>();
        var heading = "Analysis";

        void AddSection()
        {
            var content = string.Join('\n', body).Trim();
            if (content.Length > 0)
                sections.Add(new ReportAnalysisSection(heading, content));
            body.Clear();
        }

        foreach (var rawLine in commentary.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            var normalized = line.TrimStart('#', '*', ' ').TrimEnd(':', '*', ' ');
            var knownHeading = KnownHeadings.FirstOrDefault(candidate =>
                candidate.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (knownHeading is not null)
            {
                AddSection();
                heading = knownHeading;
            }
            else
            {
                body.Add(line);
            }
        }

        AddSection();
        return sections;
    }
}
