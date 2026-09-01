using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Teacher;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class LessonsController : TeacherControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LessonsController(
        AppDbContext context,
        OwnershipHelper ownership,
        IStringLocalizer<SharedResource> localizer)
        : base(context, ownership)
    {
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var lessons = await Context.Lessons
            .Where(l => l.TeacherId == teacherId)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }

    public async Task<IActionResult> Details(int id)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
                .ThenInclude(a => a.Student)
            .Include(l => l.Grades)
                .ThenInclude(g => g.Student)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        return View(lesson);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        await LoadFormDataAsync(teacherId.Value);

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
    public async Task<IActionResult> Create(LessonFormViewModel model)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                _localizer["End time must be later than start time."].Value);
        }

        if (!await TeacherMatchesAsync(teacherId.Value, model.GroupId, model.SubjectId))
        {
            ModelState.AddModelError(
                nameof(model.GroupId),
                _localizer["Your group and subject selection must match one of your assignments."].Value);
        }

        await ValidateTimeConflictsAsync(model, teacherId.Value);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(teacherId.Value);

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
            TeacherId = teacherId.Value,
            SubjectId = model.SubjectId
        };

        Context.Lessons.Add(lesson);

        await Context.SaveChangesAsync();

        TempData["Success"] = _localizer["Lesson created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var owns = await Ownership.TeacherOwnsLessonAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons.FirstOrDefaultAsync(l => l.Id == id);

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
            SubjectId = lesson.SubjectId
        };

        await LoadFormDataAsync(teacherId.Value);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LessonFormViewModel model)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var owns = await Ownership.TeacherOwnsLessonAsync(User, model.Id);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons.FirstOrDefaultAsync(l => l.Id == model.Id);

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

        if (!await TeacherMatchesAsync(teacherId.Value, model.GroupId, model.SubjectId))
        {
            ModelState.AddModelError(
                nameof(model.GroupId),
                _localizer["Your group and subject selection must match one of your assignments."].Value);
        }

        await ValidateTimeConflictsAsync(model, teacherId.Value);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(teacherId.Value);

            return View(model);
        }

        lesson.StartTime = MakeUtc(model.StartTime);
        lesson.EndTime = MakeUtc(model.EndTime);
        lesson.Topic = string.IsNullOrWhiteSpace(model.Topic)
            ? null
            : model.Topic.Trim();
        lesson.GroupId = model.GroupId;
        lesson.SubjectId = model.SubjectId;

        await Context.SaveChangesAsync();

        TempData["Success"] = _localizer["Lesson updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormDataAsync(int teacherId)
    {
        var teacher = await Context.Teachers
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstAsync(t => t.Id == teacherId);

        ViewBag.Groups = teacher.Groups.OrderBy(g => g.Name).ToList();
        ViewBag.Subjects = teacher.Subjects.OrderBy(s => s.Name).ToList();
    }

    /// <summary>
    /// Once this teacher's time is booked for a lesson, nobody else can book
    /// that same teacher - or that same group - for an overlapping time.
    /// Mirrors the identical check on the Admin side.
    /// </summary>
    private async Task ValidateTimeConflictsAsync(LessonFormViewModel model, int teacherId)
    {
        if (model.EndTime <= model.StartTime)
        {
            return;
        }

        var start = MakeUtc(model.StartTime);
        var end = MakeUtc(model.EndTime);

        var teacherConflict = await Context.Lessons
            .Where(l =>
                l.Id != model.Id &&
                l.TeacherId == teacherId &&
                l.StartTime < end &&
                start < l.EndTime)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .FirstOrDefaultAsync();

        if (teacherConflict != null)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                _localizer[
                    "You already have {0} with {1} at {2}–{3}.",
                    teacherConflict.Subject.Name,
                    teacherConflict.Group.Name,
                    teacherConflict.StartTime.ToString("HH:mm"),
                    teacherConflict.EndTime.ToString("HH:mm")].Value);

            return;
        }

        var groupConflict = await Context.Lessons
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

    private async Task<bool> TeacherMatchesAsync(int teacherId, int groupId, int subjectId)
    {
        return await Context.Teachers
            .AnyAsync(t =>
                t.Id == teacherId &&
                t.Groups.Any(g => g.Id == groupId) &&
                t.Subjects.Any(s => s.Id == subjectId));
    }

    private static DateTime MakeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
