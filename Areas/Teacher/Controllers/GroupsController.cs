using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class GroupsController : TeacherControllerBase
{
    private readonly GroupJournalService _journalService;

    public GroupsController(
        AppDbContext context,
        OwnershipHelper ownership,
        GroupJournalService journalService)
        : base(context, ownership)
    {
        _journalService = journalService;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var groups = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }

    public async Task<IActionResult> Details(int id)
    {
        var owns = await Ownership.TeacherOwnsGroupAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var group = await Context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var subjects = await Context.Schedules
            .Where(s => s.GroupId == id)
            .Select(s => s.Subject.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        ViewBag.Subjects = subjects;

        return View(group);
    }

    [HttpGet]
    public async Task<IActionResult> Journal(int id, int weeks = 8)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var owns = await Ownership.TeacherOwnsGroupAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var model = await _journalService.BuildAsync(
            id,
            weeks,
            teacherId.Value);

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
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var owns = await Ownership.TeacherOwnsGroupAsync(User, model.GroupId);

        if (!owns)
        {
            return Forbid();
        }

        var isRussian =
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

        if (!ModelState.IsValid)
        {
            TempData["Error"] = isRussian
                ? "В журнале есть некорректные значения."
                : "Journal contains invalid values.";

            return RedirectToAction(
                nameof(Journal),
                new { id = model.GroupId, weeks = model.Weeks });
        }

        var saved = await _journalService.SaveAsync(
            model.GroupId,
            model.Entries,
            teacherId.Value);

        if (!saved)
        {
            return NotFound();
        }

        TempData["Success"] = isRussian
            ? "Журнал успешно сохранён."
            : "Journal saved successfully.";

        return RedirectToAction(
            nameof(Journal),
            new { id = model.GroupId, weeks = model.Weeks });
    }
}
