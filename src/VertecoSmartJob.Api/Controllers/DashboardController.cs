using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/dashboard")]
public class DashboardController(JobCardService jobCardService, ITechnicianRepository technicians) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats(CancellationToken ct)
    {
        var jobs = await jobCardService.GetDashboardSnapshotAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(2));
        var techs = await technicians.ListAsync(ct);

        return Ok(new DashboardStatsDto
        {
            ActiveJobCards = jobs.Count(j => j.Status is JobStatus.Assigned or JobStatus.Accepted or JobStatus.InProgress or JobStatus.Reopened),
            DueTodayCount = jobs.Count(j => j.ScheduledDate == today),
            FlaggedJobCards = jobs.Count(j => j.IsFlagged),
            TechniciansAvailable = techs.Count(t => t.Availability == TechnicianAvailability.Available)
        });
    }

    [HttpGet("recent-jobs")]
    public async Task<ActionResult<List<JobCardSummaryDto>>> RecentJobs(CancellationToken ct)
    {
        var jobs = await jobCardService.GetDashboardSnapshotAsync(ct);
        return Ok(jobs.Take(8).Select(j => j.ToSummaryDto()).ToList());
    }

    [HttpGet("status-board")]
    public async Task<ActionResult<List<JobCardSummaryDto>>> StatusBoard(CancellationToken ct)
    {
        var jobs = await jobCardService.GetDashboardSnapshotAsync(ct);
        return Ok(jobs.Select(j => j.ToSummaryDto()).ToList());
    }
}
