using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class LessonsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LessonsController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? date,
        int? groupId)
    {
        var query = _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(l =>
                l.Subject.Name.ToLower().Contains(value) ||
                l.Group.Name.ToLower().Contains(value) ||
                l.Teacher.ApplicationUser.FullName.ToLower().Contains(value) ||
                (l.Topic != null &&
                 l.Topic.ToLower().Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(date) &&
            DateOnly.TryParse(date, out var parsedDate))
        {
            var start = DateTime.SpecifyKind(
                parsedDate.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);

            var end = start.AddDays(1);

            query = query.Where(l =>
                l.StartTime >= start &&
                l.StartTime < end);
        }

        if (groupId.HasValue)
        {
            query = query.Where(l =>
                l.GroupId == groupId.Value);
        }

        ViewBag.Search = search;
        ViewBag.Date = date;
        ViewBag.GroupId = groupId;

        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();

        var lessons = await query
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        return View(lesson);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();

        var now = DateTime.UtcNow;

        var model = new LessonFormViewModel
        {
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(
        LessonFormViewModel model)
    {
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                _localizer["End time must be later than start time."].Value);
        }

        if (!await TeacherMatchesAsync(
                model.TeacherId,
                model.GroupId,
                model.SubjectId))
        {
            ModelState.AddModelError(
                nameof(model.TeacherId),
                _localizer["Selected teacher must be assigned to this group and subject."].Value);
        }

        await ValidateTimeConflictsAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();

            return View(model);
        }

        var lesson = new Lesson
        {
            StartTime = MakeUtc(model.StartTime),
            EndTime = MakeUtc(model.EndTime),
            Topic = string.IsNullOrWhiteSpace(model.Topic)
                ? null
                : model.Topic.Trim(),
            GroupId = model.GroupId,
            TeacherId = model.TeacherId,
            SubjectId = model.SubjectId
        };

        _context.Lessons.Add(lesson);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Lesson created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        var model = new LessonFormViewModel
        {
            Id = lesson.Id,
            StartTime = lesson.StartTime,
            EndTime = lesson.EndTime,
            Topic = lesson.Topic,
            GroupId = lesson.GroupId,
            TeacherId = lesson.TeacherId,
            SubjectId = lesson.SubjectId
        };

        await LoadFormDataAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(
        LessonFormViewModel model)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == model.Id);

        if (lesson == null)
        {
            return NotFound();
        }

        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                _localizer["End time must be later than start time."].Value);
        }

        if (!await TeacherMatchesAsync(
                model.TeacherId,
                model.GroupId,
                model.SubjectId))
        {
            ModelState.AddModelError(
                nameof(model.TeacherId),
                _localizer["Selected teacher must be assigned to this group and subject."].Value);
        }

        await ValidateTimeConflictsAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();

            return View(model);
        }

        lesson.StartTime = MakeUtc(model.StartTime);
        lesson.EndTime = MakeUtc(model.EndTime);
        lesson.Topic = string.IsNullOrWhiteSpace(model.Topic)
            ? null
            : model.Topic.Trim();
        lesson.GroupId = model.GroupId;
        lesson.TeacherId = model.TeacherId;
        lesson.SubjectId = model.SubjectId;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Lesson updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        return View(lesson);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        var hasAttendance = await _context.Attendances
            .AnyAsync(a => a.LessonId == id);

        var hasGrades = await _context.Grades
            .AnyAsync(g => g.LessonId == id);

        if (hasAttendance || hasGrades)
        {
            TempData["Error"] =
                _localizer["This lesson cannot be deleted because attendance or grades are connected to it."].Value;

            return RedirectToAction(nameof(Index));
        }

        _context.Lessons.Remove(lesson);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Lesson deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
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

    /// <summary>
    /// Once a teacher's time is booked for a lesson, nobody else can book
    /// that same teacher - or that same group - for an overlapping time.
    /// Mirrors the conflict check already used for the weekly Schedule
    /// template, but against actual dated lessons.
    /// </summary>
    private async Task ValidateTimeConflictsAsync(LessonFormViewModel model)
    {
        if (model.EndTime <= model.StartTime)
        {
            // Already reported by the caller; nothing sensible to check here.
            return;
        }

        var start = MakeUtc(model.StartTime);
        var end = MakeUtc(model.EndTime);

        var teacherConflict = await _context.Lessons
            .Where(l =>
                l.Id != model.Id &&
                l.TeacherId == model.TeacherId &&
                l.StartTime < end &&
                start < l.EndTime)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
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

            return;
        }

        var groupConflict = await _context.Lessons
            .Where(l =>
                l.Id != model.Id &&
                l.GroupId == model.GroupId &&
                l.StartTime < end &&
                start < l.EndTime)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
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

    private async Task<bool> TeacherMatchesAsync(
        int teacherId,
        int groupId,
        int subjectId)
    {
        return await _context.Teachers
            .AnyAsync(t =>
                t.Id == teacherId &&
                t.Groups.Any(g => g.Id == groupId) &&
                t.Subjects.Any(s => s.Id == subjectId));
    }

    private static DateTime MakeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }
}