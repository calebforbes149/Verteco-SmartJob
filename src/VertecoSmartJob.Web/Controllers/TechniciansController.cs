using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class TechniciansController(TechniciansApiClient technicians, UsersApiClient users) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var result = await technicians.SearchAsync(q, pageSize: 100, ct: ct);
        ViewData["Q"] = q;
        return View(result.Items);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var technician = await technicians.GetByIdAsync(id, ct);
        if (technician is null) return NotFound();
        var account = technician.UserId is { } userId ? await users.GetByIdAsync(userId, ct) : null;
        return View(new TechnicianDetailsViewModel { Technician = technician, Account = account });
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public IActionResult Create() => View(new TechnicianFormViewModel());

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TechnicianFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var created = await technicians.CreateAsync(new CreateTechnicianRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Skills = model.SkillsList(),
                Availability = model.Availability
            }, ct);
            TempData["Toast"] = $"Technician {created.FullName} created, with a linked user account.";
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
        var technician = await technicians.GetByIdAsync(id, ct);
        if (technician is null) return NotFound();
        return View(new TechnicianFormViewModel
        {
            Id = technician.Id,
            FullName = technician.FullName,
            Email = technician.Email,
            Phone = technician.Phone,
            Availability = technician.Availability,
            SkillsText = string.Join(", ", technician.Skills)
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TechnicianFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await technicians.PatchAsync(id, new PatchTechnicianRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Skills = model.SkillsList(),
                Availability = model.Availability
            }, ct);
            TempData["Toast"] = "Technician updated.";
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
    public async Task<IActionResult> SetPassword(Guid id, Guid userId, string newPassword,
        string confirmPassword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8 ||
            newPassword != confirmPassword)
        {
            TempData["Error"] = "Passwords must match and contain at least 8 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var technician = await technicians.GetByIdAsync(id, ct);
        if (technician?.UserId != userId)
            return NotFound();

        try
        {
            await users.SetPasswordAsync(userId, newPassword, ct);
            TempData["Toast"] = "Technician password set. Share it with the technician securely.";
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
    public async Task<IActionResult> Suspend(Guid id, Guid userId, CancellationToken ct)
    {
        try { await users.SuspendAsync(userId, ct); TempData["Toast"] = "Account suspended."; }
        catch (ApiException ex) { TempData["Error"] = ex.Detail ?? ex.Title; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(Guid id, Guid userId, CancellationToken ct)
    {
        try { await users.ReactivateAsync(userId, ct); TempData["Toast"] = "Account reactivated."; }
        catch (ApiException ex) { TempData["Error"] = ex.Detail ?? ex.Title; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, Guid userId, CancellationToken ct)
    {
        try { await users.DeactivateAsync(userId, ct); TempData["Toast"] = "Account deactivated."; }
        catch (ApiException ex) { TempData["Error"] = ex.Detail ?? ex.Title; }
        return RedirectToAction(nameof(Details), new { id });
    }
}