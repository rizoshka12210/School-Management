using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ScheduleEntity = SchoolManagementSystem.Web.Models.Entities.Schedule;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class ScheduleController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ScheduleController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? day, int? groupId)
    {
        var query = _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(day) &&
            Enum.TryParse<DayOfWeek>(day, true, out var selectedDay))
        {
            query = query.Where(s => s.DayOfWeek == selectedDay);
        }

        if (groupId.HasValue)
        {
            query = query.Where(s => s.GroupId == groupId.Value);
        }

        ViewBag.Day = day;
        ViewBag.GroupId = groupId;
        ViewBag.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();

        var schedule = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return View(schedule);
    }

    public async Task<IActionResult> Details(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule == null ? NotFound() : View(schedule);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();
        return View(new ScheduleFormViewModel
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(ScheduleFormViewModel model)
    {
        await ValidateAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return View(model);
        }

        var schedule = new ScheduleEntity
        {
            DayOfWeek = model.DayOfWeek,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            GroupId = model.GroupId,
            TeacherId = model.TeacherId,
            SubjectId = model.SubjectId
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        TempData["Success"] = _localizer["Schedule entry created successfully."].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id);
        if (schedule == null) return NotFound();

        var model = new ScheduleFormViewModel
        {
            Id = schedule.Id,
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            GroupId = schedule.GroupId,
            TeacherId = schedule.TeacherId,
            SubjectId = schedule.SubjectId
        };

        await LoadFormDataAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(ScheduleFormViewModel model)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == model.Id);
        if (schedule == null) return NotFound();

        await ValidateAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return View(model);
        }

        schedule.DayOfWeek = model.DayOfWeek;
        schedule.StartTime = model.StartTime;
        schedule.EndTime = model.EndTime;
        schedule.GroupId = model.GroupId;
        schedule.TeacherId = model.TeacherId;
        schedule.SubjectId = model.SubjectId;

        await _context.SaveChangesAsync();

        TempData["Success"] = _localizer["Schedule updated successfully."].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule == null ? NotFound() : View(schedule);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id);
        if (schedule == null) return NotFound();

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        TempData["Success"] = _localizer["Schedule entry deleted successfully."].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(ScheduleFormViewModel model)
    {
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                _localizer["End time must be later than start time."].Value);
        }

        var teacherMatches = await _context.Teachers.AnyAsync(t =>
            t.Id == model.TeacherId &&
            t.Groups.Any(g => g.Id == model.GroupId) &&
            t.Subjects.Any(s => s.Id == model.SubjectId));

        if (!teacherMatches)
        {
            ModelState.AddModelError(
                nameof(model.TeacherId),
                _localizer["Selected teacher must be assigned to this group and subject."].Value);
        }

        var teacherConflict = await _context.Schedules
            .Where(s =>
                s.Id != model.Id &&
                s.DayOfWeek == model.DayOfWeek &&
                s.TeacherId == model.TeacherId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Subject)
            .Include(s => s.Group)
            .FirstOrDefaultAsync();

        if (teacherConflict != null)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                _localizer[
                    "Teacher {0} already has {1} with {2} at {3}–{4}.",
                    teacherConflict.Teacher.ApplicationUser.FullName,
                    teacherConflict.Subject.Name,
                    teacherConflict.Group.Name,
                    teacherConflict.StartTime.ToString("HH:mm"),
                    teacherConflict.EndTime.ToString("HH:mm")].Value);
        }

        var groupConflict = await _context.Schedules
            .Where(s =>
                s.Id != model.Id &&
                s.DayOfWeek == model.DayOfWeek &&
                s.GroupId == model.GroupId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Subject)
            .Include(s => s.Group)
            .FirstOrDefaultAsync();

        if (groupConflict != null)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                _localizer[
                    "Group {0} already has {1} with {2} at {3}–{4}.",
                    groupConflict.Group.Name,
                    groupConflict.Subject.Name,
                    groupConflict.Teacher.ApplicationUser.FullName,
                    groupConflict.StartTime.ToString("HH:mm"),
                    groupConflict.EndTime.ToString("HH:mm")].Value);
        }
    }

    private async Task LoadFormDataAsync()
    {
        ViewBag.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();
        ViewBag.Subjects = await _context.Subjects.OrderBy(s => s.Name).ToListAsync();
    }
}
