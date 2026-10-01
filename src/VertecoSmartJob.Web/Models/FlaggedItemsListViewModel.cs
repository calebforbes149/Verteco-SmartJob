using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Web.Models;

public class FlaggedItemsListViewModel
{
    public string? Status { get; set; } = "open";
    public PagedResponse<CorrectiveActionDto> Results { get; set; } = new();
}