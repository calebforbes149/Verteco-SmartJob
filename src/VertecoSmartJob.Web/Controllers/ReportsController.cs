using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize(Roles = "Manager")]
public class ReportsController(ReportsApiClient reports) : Controller
{
    public async Task<IActionResult> Index(int? year, CancellationToken ct)
    {
        var selectedYear = year ?? DateTime.UtcNow.Year;
        var model = new ReportsListViewModel
        {
            Year = selectedYear,
            Results = await reports.SearchAsync(selectedYear, pageSize: 12, ct: ct)
        };
        return View(model);
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        try
        {
            var report = await reports.GetByIdAsync(id, ct);
            return report is null ? NotFound() : View(report);
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Export(string id, string format, CancellationToken ct)
    {
        try
        {
            var (bytes, contentType, fileName) = await reports.ExportAsync(id, format, ct);
            return File(bytes, contentType, fileName);
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}