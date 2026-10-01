using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class CustomersController(CustomersApiClient customers) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var result = await customers.SearchAsync(q, pageSize: 100, ct: ct);
        ViewData["Q"] = q;
        return View(result.Items);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : View(customer);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public IActionResult Create() => View(new CustomerFormViewModel());

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var created = await customers.CreateAsync(new CreateCustomerRequest
            {
                Name = model.Name,
                Industry = model.Industry,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone
            }, ct);
            TempData["Toast"] = $"Customer {created.Name} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Detail ?? ex.Title);
            return View(model);
        }
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        if (customer is null) return NotFound();
        return View(new CustomerFormViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Industry = customer.Industry,
            ContactName = customer.ContactName,
            ContactEmail = customer.ContactEmail,
            ContactPhone = customer.ContactPhone
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CustomerFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await customers.PatchAsync(id, new PatchCustomerRequest
            {
                Name = model.Name,
                Industry = model.Industry,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone
            }, ct);
            TempData["Toast"] = "Customer updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Detail ?? ex.Title);
            return View(model);
        }
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInline([FromForm] InlineCreateCustomerRequest form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return Json(new InlineCreateResult { Ok = false, Error = firstError ?? "Check the customer details and try again." });
        }

        try
        {
            var created = await customers.CreateAsync(new CreateCustomerRequest
            {
                Name = form.Name,
                Industry = form.Industry,
                ContactName = form.ContactName,
                ContactEmail = form.ContactEmail,
                ContactPhone = form.ContactPhone
            }, ct);

            return Json(new InlineCreateResult { Ok = true, Id = created.Id, Name = created.Name });
        }
        catch (ApiException ex)
        {
            return Json(new InlineCreateResult { Ok = false, Error = ex.Detail ?? ex.Title });
        }
    }
}