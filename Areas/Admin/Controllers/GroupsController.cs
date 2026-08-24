using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class GroupsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly GroupJournalService _journalService;

    public GroupsController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer,
        GroupJournalService journalService)
    {
        _context = context;
        _localizer = localizer;
        _journalService = journalService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(g =>
                g.Name.ToLower().Contains(value));
        }

        ViewBag.Search = search;

        var groups = await query
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }

    public async Task<IActionResult> Details(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }

    [HttpGet]
    public async Task<IActionResult> Journal(int id, int weeks = 8)
    {
        var model = await _journalService.BuildAsync(id, weeks);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Journal(GroupJournalSaveViewModel model)
    {
        var isRussian =
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

        if (!ModelState.IsValid)
        {
            TempData["Error"] = isRussian
                ? "В журнале есть некорректные значения."
                : _localizer["Journal contains invalid values."].Value;

            return RedirectToAction(
                nameof(Journal),
                new { id = model.GroupId, weeks = model.Weeks });
        }

        var saved = await _journalService.SaveAsync(
            model.GroupId,
            model.Entries);

        if (!saved)
        {
            return NotFound();
        }

        TempData["Success"] = isRussian
            ? "Журнал успешно сохранён."
            : _localizer["Journal saved successfully."].Value;

        return RedirectToAction(
            nameof(Journal),
            new { id = model.GroupId, weeks = model.Weeks });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadTeachersAsync();

        return View(new GroupFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        GroupFormViewModel model)
    {
        var exists = await _context.Groups
            .AnyAsync(g =>
                g.Name.ToLower() ==
                model.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A group with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();

            return View(model);
        }

        var group = new Group
        {
            Name = model.Name.Trim()
        };

        if (model.TeacherIds != null &&
            model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t =>
                    model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                group.Teachers.Add(teacher);
            }
        }

        _context.Groups.Add(group);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Group created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var model = new GroupFormViewModel
        {
            Id = group.Id,
            Name = group.Name,
            TeacherIds = group.Teachers
                .Select(t => t.Id)
                .ToList()
        };

        await LoadTeachersAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        GroupFormViewModel model)
    {
        var group = await _context.Groups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(
                g => g.Id == model.Id);

        if (group == null)
        {
            return NotFound();
        }

        var exists = await _context.Groups
            .AnyAsync(g =>
                g.Id != model.Id &&
                g.Name.ToLower() ==
                model.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A group with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();

            return View(model);
        }

        group.Name = model.Name.Trim();

        group.Teachers.Clear();

        if (model.TeacherIds != null &&
            model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t =>
                    model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                group.Teachers.Add(teacher);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Group updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var hasLessons = await _context.Lessons
            .AnyAsync(l => l.GroupId == id);

        var hasSchedules = await _context.Schedules
            .AnyAsync(s => s.GroupId == id);

        if (hasLessons || hasSchedules)
        {
            TempData["Error"] =
                _localizer["This group cannot be deleted because it has lessons or schedule entries."].Value;

            return RedirectToAction(nameof(Index));
        }

        _context.Groups.Remove(group);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Group deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTeachersAsync()
    {
        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();
    }
}
