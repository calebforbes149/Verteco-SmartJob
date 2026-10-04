using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class JobCardsController(
    JobCardsApiClient jobCards,
    CustomersApiClient customers,
    SitesApiClient sites,
    JobTemplatesApiClient templates,
    TechniciansApiClient technicians) : Controller
{
    public async Task<IActionResult> Index([FromQuery] JobCardListQuery query, CancellationToken ct)
    {
        var model = new JobCardListViewModel
        {
            Query = query,
            Results = await jobCards.SearchAsync(query, ct)
        };
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var job = await jobCards.GetByIdAsync(id, ct);
        if (job is null) return NotFound();
        return View(job);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new CreateJobCardViewModel();
        await PopulateCreateDropdownsAsync(model, ct);
        return View(model);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateJobCardViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateDropdownsAsync(model, ct);
            return View(model);
        }

        try
        {
            var request = new CreateJobCardRequest
            {
                CustomerId = model.CustomerId!.Value,
                SiteId = model.SiteId!.Value,
                Priority = model.Priority,
                ScheduledDate = model.ScheduledDate,
                Notes = model.Notes,
                TechnicianId = model.TechnicianId
            };
            var created = await jobCards.CreateAsync(request, ct);
            TempData["Toast"] = $"Job card {created.Number} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Detail ?? ex.Title);
            await PopulateCreateDropdownsAsync(model, ct);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SitesForCustomer(Guid customerId, CancellationToken ct)
    {
        var result = await sites.ListByCustomerAsync(customerId, ct);
        return Json(result.Items.Select(s => new { id = s.Id, name = s.Name }));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, Guid technicianId, CancellationToken ct)
    {
        try
        {
            await jobCards.AssignAsync(id, technicianId, ct);
            TempData["Toast"] = "Technician assigned.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? reviewNotes, CancellationToken ct)
    {
        try
        {
            await jobCards.CompleteAsync(id, reviewNotes, ct);
            TempData["Toast"] = "Job card approved and completed.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id, string reason, CancellationToken ct)
    {
        try
        {
            await jobCards.ReopenAsync(id, reason, ct);
            TempData["Toast"] = "Job card reopened.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string reason, CancellationToken ct)
    {
        try
        {
            await jobCards.CancelAsync(id, reason, ct);
            TempData["Toast"] = "Job card cancelled.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Flag(Guid id, string reason, CancellationToken ct)
    {
        try
        {
            await jobCards.FlagAsync(id, reason, ct);
            TempData["Toast"] = "Job card flagged.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveFlag(Guid id, string resolutionNotes, CancellationToken ct)
    {
        try
        {
            await jobCards.ResolveFlagAsync(id, resolutionNotes, ct);
            TempData["Toast"] = "Flag resolved.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Detail ?? ex.Title;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateCreateDropdownsAsync(CreateJobCardViewModel model, CancellationToken ct)
    {
        var customerPage = await customers.SearchAsync(page: 1, pageSize: 200, ct: ct);
        model.Customers = customerPage.Items;

        if (model.CustomerId is { } customerId)
        {
            var sitePage = await sites.ListByCustomerAsync(customerId, ct);
            model.Sites = sitePage.Items;
        }

        model.StandardTemplate = await templates.GetStandardAsync(ct);

        var technicianPage = await technicians.SearchAsync(pageSize: 200, ct: ct);
        model.Technicians = technicianPage.Items;
    }
}