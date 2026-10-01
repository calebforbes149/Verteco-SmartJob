using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Web.Api;

namespace VertecoSmartJob.Web.Models;

public class JobCardListViewModel
{
    public JobCardListQuery Query { get; set; } = new();
    public PagedResponse<JobCardSummaryDto> Results { get; set; } = new();

    public static readonly string[] Statuses =
        ["Draft", "Assigned", "Accepted", "InProgress", "AwaitingApproval", "Completed", "Reopened", "Cancelled"];

    public static readonly string[] Priorities = ["Low", "Medium", "High", "Urgent"];
}