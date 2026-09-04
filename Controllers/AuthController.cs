using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Auth;

namespace SchoolManagementSystem.Web.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectByRole();
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var input = model.EmailOrPhone.Trim();

        var user = await _userManager.FindByEmailAsync(input)
            ?? await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == input);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer["Invalid email/phone or password."]);

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer["Too many failed attempts. Your account is temporarily locked, please try again later."]);

            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer["Invalid email/phone or password."]);

            return View(model);
        }

        TempData["OpenAiAssistant"] = "1";

        return await RedirectByRoleAsync(user);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }


    private IActionResult RedirectByRole()
    {
        if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Director))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Admin" });
        }

        if (User.IsInRole(Roles.Teacher))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Teacher" });
        }

        if (User.IsInRole(Roles.Parent))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Parent" });
        }

        return RedirectToAction(
            "Index",
            "Home");
    }


    private async Task<IActionResult> RedirectByRoleAsync(
        ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, Roles.Admin) ||
            await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Admin" });
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Teacher))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Teacher" });
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Parent))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Parent" });
        }

        return RedirectToAction(
            "Index",
            "Home");
    }
}