using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VertecoSmartJob.Web.Controllers;

[AllowAnonymous]
public class PrivacyController : Controller
{
    public IActionResult Index() => View();
}