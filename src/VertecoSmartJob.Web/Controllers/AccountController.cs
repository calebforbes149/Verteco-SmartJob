using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Auth;
using VertecoSmartJob.Web.Models;

namespace VertecoSmartJob.Web.Controllers;

public class AccountController(AuthApiClient authApi) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLocal(returnUrl);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await authApi.LoginAsync(model.Email, model.Password, ct);
        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.FullName),
            new(ClaimTypes.Email, model.Email),
            new(ClaimTypes.Role, result.Role),
            new(ApiKeyDelegatingHandler.AccessTokenClaimType, result.AccessToken)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await authApi.ForgotPasswordAsync(model.Email.Trim(), ct);
            model.IsSubmitted = true;
            return View(model);
        }
        catch (ApiException ex) when (ex.StatusCode == 429)
        {
            ModelState.AddModelError(string.Empty, "Too many requests. Please try again later.");
            return View(model);
        }
        catch (ApiException ex) when (ex.StatusCode == 503)
        {
            ModelState.AddModelError(string.Empty,
                "Password reset email is unavailable. Please contact your SmartJob administrator.");
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Reset link is missing its token.");
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await authApi.ResetPasswordAsync(model.Token, model.NewPassword, ct);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["PasswordResetSuccess"] = "Your password has been reset. You can now sign in.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiException ex) when (ex.StatusCode == 400)
        {
            ModelState.AddModelError(string.Empty, ex.Title);
            return View(model);
        }
        catch (ApiException ex) when (ex.StatusCode == 429)
        {
            ModelState.AddModelError(string.Empty, "Too many requests. Please try again later.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
