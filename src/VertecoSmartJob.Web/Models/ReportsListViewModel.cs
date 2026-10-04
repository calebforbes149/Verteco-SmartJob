using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reports;

namespace VertecoSmartJob.Web.Models;

public class ReportsListViewModel
{
    public int Year { get; set; }
    public PagedResponse<ReportDto> Results { get; set; } = new();
}