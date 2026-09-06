using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

/// <summary>
/// Site branding (name + logo) - Admin only, since Director is a
/// read-only counterpart and this changes what every visitor sees.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class SiteSettingsController : Controller
{
    private readonly SiteSettingsService _siteSettingsService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SiteSettingsController(
        SiteSettingsService siteSettingsService,
        IStringLocalizer<SharedResource> localizer)
    {
        _siteSettingsService = siteSettingsService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _siteSettingsService.GetAsync();

        return View(new SiteSettingsViewModel
        {
            SiteName = settings.SiteName,
            LogoPath = settings.LogoPath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsViewModel model)
    {
        if (model.Logo != null)
        {
            var error = _siteSettingsService.ValidateLogo(model.Logo);

            if (error != null)
            {
                ModelState.AddModelError(nameof(model.Logo), error);
            }
        }

        if (!ModelState.IsValid)
        {
            var current = await _siteSettingsService.GetAsync();
            model.LogoPath = current.LogoPath;
            return View(model);
        }

        await _siteSettingsService.UpdateAsync(model.SiteName, model.Logo);

        TempData["Success"] = _localizer["Site settings updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }
}
