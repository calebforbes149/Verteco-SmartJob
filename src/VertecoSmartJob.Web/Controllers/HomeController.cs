using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class HomeController(DashboardApiClient dashboard) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var model = new DashboardViewModel
            {
                Stats = await dashboard.GetStatsAsync(ct),
                RecentJobs = await dashboard.GetRecentJobsAsync(ct)
            };
            return View(model);
        }
        catch (ApiException ex) when (ex.StatusCode == 403)
        {
            ViewBag.ForbiddenMessage = "Your signed-in account doesn't have Manager access, so there's no dashboard to show here yet.";
            return View(new DashboardViewModel());
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Home") });
        }
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
