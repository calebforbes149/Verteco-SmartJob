using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;

namespace VertecoSmartJob.Web.Controllers;

[Authorize(Roles = "Manager")]
public class ActivityController(AuditApiClient audit) : Controller
{
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await audit.SearchAsync(page: page, pageSize: 30, ct: ct);
        return View(result);
    }
}