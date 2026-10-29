using VertecoSmartJob.Contracts.Reports;

namespace VertecoSmartJob.Tests.Application;

public sealed class ReportAnalysisFormatterTests
{
    [Fact]
    public void Parse_SeparatesGeminiHeadingsAndKeepsNumberedActions()
    {
        const string commentary = """
            Monthly overview
            Six jobs were created.
            **Workflow bottlenecks**
            Two jobs await approval.
            Recommended actions:
            1. Review the two jobs awaiting approval.
            2. Inspect the flagged jobs.
            Data limitations
            The status counts are current, not a month-end snapshot.
            """;

        var sections = ReportAnalysisFormatter.Parse(commentary);

        Assert.Equal(4, sections.Count);
        Assert.Equal("Monthly overview", sections[0].Heading);
        Assert.Equal("Workflow bottlenecks", sections[1].Heading);
        Assert.Equal("Recommended actions", sections[2].Heading);
        Assert.Contains("1. Review", sections[2].Body);
        Assert.Contains("2. Inspect", sections[2].Body);
        Assert.Equal("Data limitations", sections[3].Heading);
    }
}
