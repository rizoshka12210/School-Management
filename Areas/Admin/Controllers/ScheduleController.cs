using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ScheduleEntity =
    SchoolManagementSystem.Web.Models.Entities.Schedule;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ScheduleController : Controller
{
    private readonly AppDbContext _context;

    public ScheduleController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? day,
        int? groupId)
    {
        var query = _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(day) &&
            Enum.TryParse<DayOfWeek>(
                day,
                true,
                out var selectedDay))
        {
            query = query.Where(s =>
                s.DayOfWeek == selectedDay);
        }

        if (groupId.HasValue)
        {
            query = query.Where(s =>
                s.GroupId == groupId.Value);
        }

        ViewBag.Day = day;
        ViewBag.GroupId = groupId;

        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();

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

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    [HttpGet]
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
    public async Task<IActionResult> Create(
        ScheduleFormViewModel model)
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

        TempData["Success"] =
            "Schedule entry created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

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
    public async Task<IActionResult> Edit(
        ScheduleFormViewModel model)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s =>
                s.Id == model.Id);

        if (schedule == null)
        {
            return NotFound();
        }

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

        TempData["Success"] =
            "Schedule updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        _context.Schedules.Remove(schedule);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Schedule entry deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(
        ScheduleFormViewModel model)
    {
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "End time must be later than start time.");
        }

        var teacherMatches =
            await _context.Teachers.AnyAsync(t =>
                t.Id == model.TeacherId &&
                t.Groups.Any(g =>
                    g.Id == model.GroupId) &&
                t.Subjects.Any(s =>
                    s.Id == model.SubjectId));

        if (!teacherMatches)
        {
            ModelState.AddModelError(
                nameof(model.TeacherId),
                "Selected teacher must be assigned to this group and subject.");
        }

        var teacherOverlap =
            await _context.Schedules.AnyAsync(s =>
                s.Id != model.Id &&
                s.DayOfWeek == model.DayOfWeek &&
                s.TeacherId == model.TeacherId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime);

        if (teacherOverlap)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                "This teacher already has another lesson during this time.");
        }

        var groupOverlap =
            await _context.Schedules.AnyAsync(s =>
                s.Id != model.Id &&
                s.DayOfWeek == model.DayOfWeek &&
                s.GroupId == model.GroupId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime);

        if (groupOverlap)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                "This group already has another lesson during this time.");
        }
    }

    private async Task LoadFormDataAsync()
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();

        ViewBag.Subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}