using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize(Roles = "Manager")]
public class FlaggedItemsController(CorrectiveActionsApiClient correctiveActions) : Controller
{
    public async Task<IActionResult> Index(string? status, int page = 1, CancellationToken ct = default)
    {
        status ??= "open";
        var model = new FlaggedItemsListViewModel
        {
            Status = status,
            Results = await correctiveActions.SearchAsync(
                status: string.IsNullOrWhiteSpace(status) ? null : status,
                page: page, pageSize: 20, ct: ct)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, string resolutionNotes, string? status, CancellationToken ct)
    {
        try
        {
            await correctiveActions.ResolveAsync(id, resolutionNotes, ct);
            TempData["Toast"] = "Flag resolved.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Index), new { status });
    }
}