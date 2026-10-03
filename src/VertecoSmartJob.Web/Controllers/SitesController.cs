using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class SitesController(SitesApiClient sites, CustomersApiClient customers) : Controller
{
    public async Task<IActionResult> Index(string? q, Guid? customerId, CancellationToken ct)
    {
        var result = await sites.SearchAsync(q, customerId, pageSize: 100, ct: ct);
        ViewData["Q"] = q;
        ViewData["CustomerId"] = customerId;
        return View(result.Items);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var site = await sites.GetByIdAsync(id, ct);
        return site is null ? NotFound() : View(site);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(Guid? customerId, CancellationToken ct)
    {
        var model = new SiteFormViewModel { CustomerId = customerId ?? Guid.Empty };
        await PopulateCustomersAsync(model, ct);
        return View(model);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCustomersAsync(model, ct);
            return View(model);
        }

        try
        {
            var created = await sites.CreateAsync(new CreateSiteRequest
            {
                CustomerId = model.CustomerId,
                Name = model.Name,
                Address = model.Address,
                SiteType = model.SiteType
            }, ct);
            TempData["Toast"] = $"Site {created.Name} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Detail ?? ex.Title);
            await PopulateCustomersAsync(model, ct);
            return View(model);
        }
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var site = await sites.GetByIdAsync(id, ct);
        if (site is null) return NotFound();
        var model = new SiteFormViewModel
        {
            Id = site.Id,
            CustomerId = site.CustomerId,
            Name = site.Name,
            Address = site.Address,
            SiteType = site.SiteType
        };
        await PopulateCustomersAsync(model, ct);
        return View(model);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SiteFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCustomersAsync(model, ct);
            return View(model);
        }

        try
        {
            await sites.PatchAsync(id, new PatchSiteRequest
            {
                Name = model.Name,
                Address = model.Address,
                SiteType = model.SiteType
            }, ct);
            TempData["Toast"] = "Site updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Detail ?? ex.Title);
            await PopulateCustomersAsync(model, ct);
            return View(model);
        }
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInline([FromForm] InlineCreateSiteRequest form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return Json(new InlineCreateResult { Ok = false, Error = firstError ?? "Check the site details and try again." });
        }

        try
        {
            var created = await sites.CreateAsync(new CreateSiteRequest
            {
                CustomerId = form.CustomerId,
                Name = form.Name,
                Address = form.Address,
                SiteType = form.SiteType
            }, ct);

            return Json(new InlineCreateResult { Ok = true, Id = created.Id, Name = created.Name });
        }
        catch (ApiException ex)
        {
            return Json(new InlineCreateResult { Ok = false, Error = ex.Detail ?? ex.Title });
        }
    }

    private async Task PopulateCustomersAsync(SiteFormViewModel model, CancellationToken ct)
    {
        var customerPage = await customers.SearchAsync(page: 1, pageSize: 200, ct: ct);
        model.Customers = customerPage.Items;
    }
}