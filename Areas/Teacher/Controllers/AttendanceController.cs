using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Teacher;

using AttendanceEntity = SchoolManagementSystem.Web.Models.Entities.Attendance;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class AttendanceController : TeacherControllerBase
{
    private readonly AttendanceService _attendanceService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public AttendanceController(
        AppDbContext context,
        OwnershipHelper ownership,
        AttendanceService attendanceService,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
        : base(context, ownership)
    {
        _attendanceService = attendanceService;
        _localizer = localizer;
        _activityLog = activityLog;
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
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Journal(
        int? groupId,
        int? subjectId,
        DateTime? from,
        DateTime? to)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        ViewBag.Groups = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.Subjects = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Subjects)
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (!groupId.HasValue)
        {
            ViewBag.GroupId = null;
            ViewBag.SubjectId = subjectId;

            return View();
        }

        var owns = await Ownership.TeacherOwnsGroupAsync(User, groupId.Value);

        if (!owns)
        {
            return Forbid();
        }

        var fromUtc = DateTime.SpecifyKind(
            (from ?? DateTime.UtcNow.AddMonths(-1)).Date,
            DateTimeKind.Utc);

        var toUtc = DateTime.SpecifyKind(
            (to ?? DateTime.UtcNow).Date.AddDays(1),
            DateTimeKind.Utc);

        var journal = await _attendanceService.BuildJournalAsync(
            groupId.Value,
            fromUtc,
            toUtc,
            subjectId,
            teacherId);

        ViewBag.GroupId = groupId;
        ViewBag.SubjectId = subjectId;
        ViewBag.From = fromUtc;
        ViewBag.To = toUtc;

        return View(journal);
    }

    [HttpGet]
    public async Task<IActionResult> Mark(int lessonId)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(User, lessonId);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var model = new AttendanceMarkViewModel
        {
            LessonId = lesson.Id,
            GroupName = lesson.Group.Name,
            SubjectName = lesson.Subject.Name,
            LessonDate = lesson.StartTime,

            Students = lesson.Group.Students
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Select(s =>
                {
                    var existing = lesson.Attendances
                        .FirstOrDefault(a => a.StudentId == s.Id);

                    return new AttendanceRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        Status = existing?.Status ?? AttendanceStatus.Present
                    };
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(AttendanceMarkViewModel model)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(
            User,
            model.LessonId);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
                .ThenInclude(g => g.Students)
            .Include(l => l.Attendances)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var validStudentIds = lesson.Group.Students
            .Select(s => s.Id)
            .ToHashSet();

        var teacherId = await GetTeacherIdAsync();

        var teacher = await Context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstAsync(t => t.Id == teacherId);

        foreach (var row in model.Students)
        {
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            var existing = lesson.Attendances
                .FirstOrDefault(a => a.StudentId == row.StudentId);

            if (existing != null)
            {
                existing.Status = row.Status;
            }
            else
            {
                Context.Attendances.Add(new AttendanceEntity
                {
                    LessonId = lesson.Id,
                    StudentId = row.StudentId,
                    Status = row.Status
                });
            }

            if (row.Status == AttendanceStatus.Absent)
            {
                await _activityLog.LogAsync(
                    $"Teacher {teacher.ApplicationUser.FullName} marked {row.StudentName} absent");
            }
        }

        await Context.SaveChangesAsync();

        TempData["Success"] = _localizer["Attendance saved."].Value;

        return RedirectToAction(
            nameof(Mark),
            new { lessonId = lesson.Id });
    }
}
