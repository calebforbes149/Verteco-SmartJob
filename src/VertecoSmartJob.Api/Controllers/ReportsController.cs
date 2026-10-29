using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using VertecoSmartJob.Application.Reports;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reports;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/reports")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReportDto>>> List(
        [FromQuery] int? year = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 12,
        [FromQuery] string sort = "-month", CancellationToken ct = default)
    {
        var selectedYear = year ?? DateTime.UtcNow.Year;
        if (selectedYear is < 2000 or > 2100 || page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Invalid report query", Status = 400 });
        var months = sort == "month" ? Enumerable.Range(1, 12) : Enumerable.Range(1, 12).Reverse();
        var items = new List<ReportDto>();
        foreach (var month in months.Skip((page - 1) * pageSize).Take(pageSize))
            items.Add(Map(await reports.GetStatisticsAsync(selectedYear, month, ct)));
        return Ok(new PagedResponse<ReportDto>
        {
            Items = items, TotalCount = 12, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(12.0 / pageSize)
        });
    }

    [HttpGet("{id}")]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<ReportDto>> Get(string id, CancellationToken ct)
    {
        if (!TryPeriod(id, out var year, out var month)) return BadRequest(new ProblemDetails { Title = "Report ID must be yyyy-MM", Status = 400 });
        var (stats, ai) = await reports.GetReportAsync(year, month, ct);
        var dto = Map(stats);
        dto.AiCommentary = ai.Commentary;
        dto.AiStatus = ai.Status.ToString();
        return Ok(dto);
    }

    [HttpGet("{id}/export")]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Export(string id, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!TryPeriod(id, out var year, out var month)) return BadRequest(new ProblemDetails { Title = "Report ID must be yyyy-MM", Status = 400 });
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            return File(ReportExports.Csv(await reports.GetStatisticsAsync(year, month, ct)), "text/csv", $"smartjob-report-{id}.csv");
        if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var (stats, ai) = await reports.GetReportAsync(year, month, ct);
            return File(ReportExports.Pdf(stats, ai.Commentary), "application/pdf", $"smartjob-report-{id}.pdf");
        }
        return BadRequest(new ProblemDetails { Title = "Format must be csv or pdf", Status = 400 });
    }

    private static bool TryPeriod(string id, out int year, out int month)
    {
        year = 0; month = 0;
        if (id.Length != 7 || id[4] != '-' || !int.TryParse(id[..4], out year) || !int.TryParse(id[5..], out month)) return false;
        return year is >= 2000 and <= 2100 && month is >= 1 and <= 12;
    }

    private static ReportDto Map(ReportStatistics s) => new()
    {
        Id = s.Id, FromUtc = s.FromUtc, ToUtc = s.ToUtc, JobsCreated = s.JobsCreated,
        JobsCompleted = s.JobsCompleted,
        AverageCompletionHours = s.AverageCompletionHours, StatusCounts = s.StatusCounts,
        JobsInScope = s.JobsInScope, FlaggedJobs = s.FlaggedJobs, HighPriorityJobs = s.HighPriorityJobs
    };
}
