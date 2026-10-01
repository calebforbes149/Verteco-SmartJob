using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Models;

public class HistoryViewModel
{
    public string? Serial { get; set; }
    public EquipmentDto? Equipment { get; set; }
    public bool Searched { get; set; }
    public PagedResponse<JobCardSummaryDto> Jobs { get; set; } = new();
}