using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;
using SchoolManagementSystem.Web.ViewModels.Teacher;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

/// <summary>
/// Grades are organized by group -> subject -> students, matching how
/// exam results actually belong (to a subject a teacher teaches a group,
/// not to any one lesson). Index lists the teacher's groups; Group shows
/// the student sheet for a chosen subject within that group.
/// </summary>
public class GradesController : TeacherControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ExamSheetService _examSheetService;

    public GradesController(
        AppDbContext context,
        OwnershipHelper ownership,
        IStringLocalizer<SharedResource> localizer,
        ExamSheetService examSheetService)
        : base(context, ownership)
    {
        _localizer = localizer;
        _examSheetService = examSheetService;
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

        var combos = await Context.Lessons
            .Where(l => l.TeacherId == teacherId)
            .Select(l => new { l.GroupId, l.SubjectId, l.Subject.Name })
            .Distinct()
            .ToListAsync();

        var examGrades = await Context.ExamGrades
            .Where(e => e.TeacherId == teacherId)
            .ToListAsync();

        var model = groups
            .Select(g => new GradesGroupViewModel
            {
                GroupId = g.Id,
                GroupName = g.Name,
                StudentsCount = g.Students.Count,
                Subjects = combos
                    .Where(c => c.GroupId == g.Id)
                    .Select(c => new GradesSubjectViewModel
                    {
                        SubjectId = c.SubjectId,
                        SubjectName = c.Name,
                        StudentsGraded = examGrades.Count(e =>
                            e.GroupId == g.Id &&
                            e.SubjectId == c.SubjectId &&
                            (e.Exam1.HasValue || e.Exam2.HasValue))
                    })
                    .OrderBy(s => s.SubjectName)
                    .ToList()
            })
            .Where(g => g.Subjects.Any())
            .OrderBy(g => g.GroupName)
            .ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Group(int groupId, int? subjectId)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var subjects = await Context.Lessons
            .Where(l => l.TeacherId == teacherId && l.GroupId == groupId)
            .Select(l => new { l.SubjectId, l.Subject.Name })
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (!subjects.Any())
        {
            return Forbid();
        }

        var resolvedSubjectId = subjectId.HasValue && subjects.Any(s => s.SubjectId == subjectId.Value)
            ? subjectId.Value
            : subjects.First().SubjectId;

        var model = await _examSheetService.BuildAsync(groupId, resolvedSubjectId);

        if (model == null)
        {
            return NotFound();
        }

        ViewBag.Subjects = subjects
            .Select(s => new GradesSubjectViewModel { SubjectId = s.SubjectId, SubjectName = s.Name })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Group(ExamSheetSaveViewModel model)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var teachesCombo = await Context.Lessons
            .AnyAsync(l =>
                l.TeacherId == teacherId &&
                l.GroupId == model.GroupId &&
                l.SubjectId == model.SubjectId);

        if (!teachesCombo)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = _localizer["The grade sheet contains invalid values."].Value;

            return RedirectToAction(
                nameof(Group),
                new { groupId = model.GroupId, subjectId = model.SubjectId });
        }

        await _examSheetService.SaveAsync(
            model.GroupId,
            model.SubjectId,
            teacherId.Value,
            model.Rows);

        TempData["Success"] = _localizer["Grades saved."].Value;

        return RedirectToAction(
            nameof(Group),
            new { groupId = model.GroupId, subjectId = model.SubjectId });
    }

    /// <summary>
    /// Kept so existing "grade this lesson" shortcuts (lesson list, lesson
    /// details, dashboard) still work - they just land on that lesson's
    /// group/subject sheet instead of a lesson-scoped form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Add(int lessonId)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(User, lessonId);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        return RedirectToAction(
            nameof(Group),
            new { groupId = lesson.GroupId, subjectId = lesson.SubjectId });
    }
}
