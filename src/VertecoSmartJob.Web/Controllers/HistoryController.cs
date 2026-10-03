using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class HistoryController(SitesApiClient sites, JobCardsApiClient jobCards) : Controller
{
    public async Task<IActionResult> Index(string? serial, CancellationToken ct)
    {
        var model = new HistoryViewModel { Serial = serial, Searched = !string.IsNullOrWhiteSpace(serial) };
        if (model.Searched)
        {
            model.Equipment = await sites.FindEquipmentBySerialAsync(serial!, ct);
            model.Jobs = await jobCards.SearchAsync(new JobCardListQuery
            {
                Q = serial,
                PageSize = 100,
                Sort = "-scheduledDate"
            }, ct);
        }
        return View(model);
    }
}