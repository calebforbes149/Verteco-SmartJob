using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Models;

public class DashboardViewModel
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<JobCardSummaryDto> RecentJobs { get; set; } = [];
}