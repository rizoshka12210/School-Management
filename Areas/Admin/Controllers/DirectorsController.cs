using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

/// <summary>
/// Director accounts have no domain entity of their own (no groups,
/// subjects or students to manage) - just an ApplicationUser in the
/// Director role. Only Admin can create a director account or set/reset
/// its password; a director can view this list (like every other Admin
/// page) but never mutate it.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class DirectorsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public DirectorsController(
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
    {
        _userManager = userManager;
        _localizer = localizer;
        _activityLog = activityLog;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var directors = (await _userManager.GetUsersInRoleAsync(Roles.Director))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            directors = directors.Where(u =>
                u.FullName.ToLower().Contains(value) ||
                (u.Email != null && u.Email.ToLower().Contains(value)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(value)));
        }

        ViewBag.Search = search;

        var list = directors
            .OrderBy(u => u.FullName)
            .ToList();

        return View(list);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null || !await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create()
    {
        return View(new DirectorFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(DirectorFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);

            if (existingByEmail != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "A user with this email already exists.");
            }
        }

        var existingByPhone = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

        if (existingByPhone != null)
        {
            ModelState.AddModelError(
                nameof(model.PhoneNumber),
                "A user with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hasEmail = !string.IsNullOrWhiteSpace(model.Email);

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            Email = hasEmail ? model.Email!.Trim() : null,
            UserName = hasEmail ? model.Email!.Trim() : model.PhoneNumber.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            EmailConfirmed = hasEmail
        };

        var result = await _userManager.CreateAsync(user, model.Password!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Director);

        await _activityLog.LogAsync(
            $"Admin created director account \"{user.FullName}\"");

        TempData["Success"] =
            _localizer["Director created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null || !await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return NotFound();
        }

        return View(new DirectorFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(DirectorFormViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id ?? string.Empty);

        if (user == null || !await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);

            if (existingByEmail != null && existingByEmail.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "A user with this email already exists.");
            }
        }

        var existingByPhone = await _userManager.Users
            .FirstOrDefaultAsync(u =>
                u.PhoneNumber == model.PhoneNumber &&
                u.Id != user.Id);

        if (existingByPhone != null)
        {
            ModelState.AddModelError(
                nameof(model.PhoneNumber),
                "A user with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hasEmail = !string.IsNullOrWhiteSpace(model.Email);

        user.FullName = model.FullName.Trim();
        user.Email = hasEmail ? model.Email!.Trim() : null;
        user.UserName = hasEmail ? model.Email!.Trim() : model.PhoneNumber.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.EmailConfirmed = hasEmail;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var passwordResult = await _userManager.ResetPasswordAsync(
                user, token, model.Password);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _activityLog.LogAsync(
                $"Admin reset the password for director \"{user.FullName}\"");
        }

        TempData["Success"] =
            _localizer["Director updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null || !await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null || !await _userManager.IsInRoleAsync(user, Roles.Director))
        {
            return NotFound();
        }

        await _userManager.DeleteAsync(user);

        TempData["Success"] =
            _localizer["Director deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }
}
