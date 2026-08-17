using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Parent;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class ProgressController : ParentControllerBase
{
    private readonly GradeService _gradeService;
    private readonly AttendanceService _attendanceService;

    public ProgressController(
        AppDbContext context,
        OwnershipHelper ownership,
        GradeService gradeService,
        AttendanceService attendanceService)
        : base(context, ownership)
    {
        _gradeService = gradeService;
        _attendanceService = attendanceService;
    }

    public async Task<IActionResult> Index(int? studentId)
    {
        var resolvedId =
            await ResolveStudentIdAsync(studentId);

        if (resolvedId == null)
        {
            return Forbid();
        }

        var student = await Context.Students
            .Include(s => s.Group)
            .FirstOrDefaultAsync(
                s => s.Id == resolvedId.Value);

        if (student == null)
        {
            return NotFound();
        }

        var grades =
            await _gradeService
                .GetByStudentAsync(student.Id);

        var attendances =
            await _attendanceService
                .GetByStudentAsync(student.Id);

        var presentCount = attendances.Count(a =>
            a.Status == AttendanceStatus.Present);

        var lateCount = attendances.Count(a =>
            a.Status == AttendanceStatus.Late);

        var absentCount = attendances.Count(a =>
            a.Status == AttendanceStatus.Absent);

        var excusedCount = attendances.Count(a =>
            a.Status == AttendanceStatus.Excused);

        var totalAttendance = attendances.Count;

        var attendedLessons =
            presentCount + lateCount;

        var attendanceRate =
            totalAttendance == 0
                ? 0
                : Math.Round(
                    attendedLessons * 100.0 /
                    totalAttendance,
                    1);

        var subjects = grades
            .GroupBy(g => new
            {
                g.SubjectId,
                g.Subject.Name
            })
            .Select(g => new SubjectProgressViewModel
            {
                SubjectName = g.Key.Name,

                GradesCount = g.Count(),

                AverageGrade = Math.Round(
                    g.Average(x => x.Value),
                    2)
            })
            .OrderBy(s => s.SubjectName)
            .ToList();

        var model = new ProgressViewModel
        {
            StudentId = student.Id,

            StudentName =
                $"{student.FirstName} {student.LastName}",

            GroupName = student.Group?.Name,

            GradesCount = grades.Count,

            AverageGrade = grades.Count == 0
                ? 0
                : Math.Round(
                    grades.Average(g => g.Value),
                    2),

            TotalLessons = totalAttendance,

            PresentCount = presentCount,

            LateCount = lateCount,

            AbsentCount = absentCount,

            ExcusedCount = excusedCount,

            AttendanceRate = attendanceRate,

            Subjects = subjects
        };

        return View(model);
    }
}