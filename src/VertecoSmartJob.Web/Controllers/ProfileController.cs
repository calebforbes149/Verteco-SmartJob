using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VertecoSmartJob.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    public IActionResult Index() => View();
}