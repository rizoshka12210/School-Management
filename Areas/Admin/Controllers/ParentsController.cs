using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ParentEntity =
    SchoolManagementSystem.Web.Models.Entities.Parent;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class ParentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public ParentsController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
    {
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _activityLog = activityLog;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
                .ThenInclude(s => s.Group)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(p =>
                p.ApplicationUser.FullName.ToLower().Contains(value) ||
                (p.ApplicationUser.Email != null &&
                 p.ApplicationUser.Email.ToLower().Contains(value)));
        }

        ViewBag.Search = search;

        var parents = await query
            .OrderBy(p => p.ApplicationUser.FullName)
            .ToListAsync();

        return View(parents);
    }

    public async Task<IActionResult> Details(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
                .ThenInclude(s => s.Group)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        ViewBag.Summons = await _context.ParentSummons
            .Where(s => s.ParentId == id)
            .OrderByDescending(s => s.ScheduledAt)
            .Take(10)
            .ToListAsync();

        return View(parent);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Summon(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        return View(new ParentSummonFormViewModel
        {
            ParentId = parent.Id,
            ParentName = parent.ApplicationUser.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Summon(ParentSummonFormViewModel model)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .FirstOrDefaultAsync(p => p.Id == model.ParentId);

        if (parent == null)
        {
            return NotFound();
        }

        if (model.ScheduledAt <= DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(model.ScheduledAt),
                _localizer["The scheduled time must be in the future."].Value);
        }

        if (!ModelState.IsValid)
        {
            model.ParentName = parent.ApplicationUser.FullName;
            return View(model);
        }

        var summon = new ParentSummon
        {
            ParentId = parent.Id,
            ScheduledAt = DateTime.SpecifyKind(model.ScheduledAt, DateTimeKind.Utc),
            Message = string.IsNullOrWhiteSpace(model.Message)
                ? null
                : model.Message.Trim()
        };

        _context.ParentSummons.Add(summon);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            $"Admin summoned parent \"{parent.ApplicationUser.FullName}\" for {summon.ScheduledAt:g}");

        TempData["Success"] =
            _localizer["Parent summoned successfully."].Value;

        return RedirectToAction(nameof(Details), new { id = parent.Id });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        await LoadStudentsAsync();

        return View(new ParentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(
        ParentFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();

            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.Email.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(
            user,
            model.Password!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await LoadStudentsAsync();

            return View(model);
        }

        await _userManager.AddToRoleAsync(
            user,
            Roles.Parent);

        var parent = new ParentEntity
        {
            ApplicationUserId = user.Id
        };

        if (model.StudentIds != null &&
            model.StudentIds.Count > 0)
        {
            var students = await _context.Students
                .Where(s =>
                    model.StudentIds.Contains(s.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                parent.Students.Add(student);
            }
        }

        _context.Parents.Add(parent);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Parent created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        var model = new ParentFormViewModel
        {
            Id = parent.Id,
            FullName = parent.ApplicationUser.FullName,
            Email = parent.ApplicationUser.Email ?? string.Empty,
            StudentIds = parent.Students
                .Select(s => s.Id)
                .ToList()
        };

        await LoadStudentsAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(
        ParentFormViewModel model)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(
                p => p.Id == model.Id);

        if (parent == null)
        {
            return NotFound();
        }

        var existingUser =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (existingUser != null &&
            existingUser.Id != parent.ApplicationUserId)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();

            return View(model);
        }

        var user = parent.ApplicationUser;

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await LoadStudentsAsync();

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);

            var passwordResult =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.Password);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await LoadStudentsAsync();

                return View(model);
            }
        }

        parent.Students.Clear();

        if (model.StudentIds != null &&
            model.StudentIds.Count > 0)
        {
            var students = await _context.Students
                .Where(s =>
                    model.StudentIds.Contains(s.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                parent.Students.Add(student);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Parent updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        return View(parent);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        var user = parent.ApplicationUser;

        parent.Students.Clear();

        _context.Parents.Remove(parent);

        await _context.SaveChangesAsync();

        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

        TempData["Success"] =
            _localizer["Parent deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStudentsAsync()
    {
        ViewBag.Students = await _context.Students
            .Include(s => s.Group)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();
    }
}