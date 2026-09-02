using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class ChildController : ParentControllerBase
{
    private readonly AchievementService _achievements;

    public ChildController(
        AppDbContext context,
        OwnershipHelper ownership,
        AchievementService achievements)
        : base(context, ownership)
    {
        _achievements = achievements;
    }

    public async Task<IActionResult> Details(int? studentId)
    {
        var resolvedId = await ResolveStudentIdAsync(studentId);

        if (resolvedId == null)
        {
            return Forbid();
        }

        var student = await Context.Students
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == resolvedId);

        if (student == null)
        {
            return NotFound();
        }

        var attendances = await Context.Attendances
            .Where(a => a.StudentId == resolvedId)
            .ToListAsync();

        var total = attendances.Count;

        var present = attendances
            .Count(a => a.Status == AttendanceStatus.Present);

        var grades = await Context.Grades
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Where(g => g.StudentId == resolvedId)
            .ToListAsync();

        var examGrades = await Context.ExamGrades
            .Where(e => e.StudentId == resolvedId)
            .ToListAsync();

        ViewBag.TeacherComments = grades
            .Where(g => !string.IsNullOrWhiteSpace(g.Comment))
            .OrderByDescending(g => g.Date)
            .Take(6)
            .Select(g => new TeacherCommentViewModel
            {
                TeacherName = g.Teacher.ApplicationUser.FullName,
                SubjectName = g.Subject.Name,
                Comment = g.Comment!,
                Date = g.Date
            })
            .ToList();

        ViewBag.TotalLessons = total;
        ViewBag.PresentCount = present;

        ViewBag.AttendanceRate = total == 0
            ? 0
            : Math.Round(present * 100.0 / total, 1);

        ViewBag.AverageGrade = GradeAveragingHelper.Combine(
            grades.Select(g => g.Value),
            examGrades.Select(e => e.Average)) ?? 0;

        ViewBag.Achievements = await _achievements.GetBadgesAsync(resolvedId.Value);

        return View(student);
    }
}
