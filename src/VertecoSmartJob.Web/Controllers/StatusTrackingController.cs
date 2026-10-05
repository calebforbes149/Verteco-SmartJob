using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;

namespace VertecoSmartJob.Web.Controllers;

[Authorize(Roles = "Manager")]
public class StatusTrackingController(DashboardApiClient dashboard) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            return View(await dashboard.GetStatusBoardAsync(ct));
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "StatusTracking") });
        }
    }
}
