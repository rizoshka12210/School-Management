using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;
using SchoolManagementSystem.Web.ViewModels.Teacher;

using GradeEntity = SchoolManagementSystem.Web.Models.Entities.Grade;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class GradesController : TeacherControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;
    private readonly ExamSheetService _examSheetService;

    public GradesController(
        AppDbContext context,
        OwnershipHelper ownership,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog,
        ExamSheetService examSheetService)
        : base(context, ownership)
    {
        _localizer = localizer;
        _activityLog = activityLog;
        _examSheetService = examSheetService;
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
            .Include(l => l.Grades)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Add(int lessonId)
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
            .Include(l => l.Grades)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var examGrades = await Context.ExamGrades
            .Where(e => e.GroupId == lesson.GroupId && e.SubjectId == lesson.SubjectId)
            .ToListAsync();

        var model = new GradeEntryViewModel
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
                    var existing = lesson.Grades
                        .FirstOrDefault(g => g.StudentId == s.Id);

                    var exam = examGrades
                        .FirstOrDefault(e => e.StudentId == s.Id);

                    return new GradeRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        Value = existing?.Value,
                        Comment = existing?.Comment,
                        Exam1 = exam?.Exam1,
                        Exam2 = exam?.Exam2
                    };
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(GradeEntryViewModel model)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(
            User,
            model.LessonId);

        if (!owns)
        {
            return Forbid();
        }

        var teacherId = await GetTeacherIdAsync();

        var lesson = await Context.Lessons
            .Include(l => l.Group)
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Grades)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.GroupName = lesson.Group.Name;
            model.SubjectName = lesson.Subject.Name;
            model.LessonDate = lesson.StartTime;

            return View(model);
        }

        var validStudentIds = lesson.Group.Students
            .Select(s => s.Id)
            .ToHashSet();

        var lessonDate = DateTime.SpecifyKind(
            lesson.StartTime.Date,
            DateTimeKind.Utc);

        var teacher = await Context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstAsync(t => t.Id == teacherId);

        foreach (var row in model.Students)
        {
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            if (!row.Value.HasValue)
            {
                continue;
            }

            var existing = lesson.Grades
                .FirstOrDefault(g => g.StudentId == row.StudentId);

            if (existing != null)
            {
                existing.Value = row.Value.Value;
                existing.Comment = row.Comment;
            }
            else
            {
                Context.Grades.Add(new GradeEntity
                {
                    LessonId = lesson.Id,
                    StudentId = row.StudentId,
                    SubjectId = lesson.SubjectId,
                    TeacherId = teacherId!.Value,
                    Value = row.Value.Value,
                    Date = lessonDate,
                    Comment = row.Comment
                });
            }

            await _activityLog.LogAsync(
                $"Teacher {teacher.ApplicationUser.FullName} added grade {row.Value.Value} to {row.StudentName}");
        }

        await Context.SaveChangesAsync();

        await _examSheetService.SaveAsync(
            lesson.GroupId,
            lesson.SubjectId,
            teacherId!.Value,
            model.Students.Select(row => new ExamSheetRowViewModel
            {
                StudentId = row.StudentId,
                Exam1 = row.Exam1,
                Exam2 = row.Exam2
            }));

        TempData["Success"] = _localizer["Grades saved."].Value;

        return RedirectToAction(
            nameof(Add),
            new { lessonId = lesson.Id });
    }
}
