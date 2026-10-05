using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class ProfileController(ProfileApiClient profiles) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await profiles.GetAsync(ct);
        return View(new ProfileViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Phone = profile.Phone,
            Role = profile.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return View(model);
        }

        try
        {
            var updated = await profiles.UpdateAsync(new UpdateOwnProfileRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                CurrentPassword = model.CurrentPassword
            }, ct);

            var claims = User.Claims
                .Where(c => c.Type != ClaimTypes.Name && c.Type != ClaimTypes.Email)
                .ToList();
            claims.Add(new Claim(ClaimTypes.Name, updated.FullName));
            claims.Add(new Claim(ClaimTypes.Email, updated.Email));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme)));

            TempData["ProfileSaved"] = "Your profile was saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            ModelState.AddModelError(string.Empty, ex.Title);
            model.Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return View(model);
        }
    }
}
