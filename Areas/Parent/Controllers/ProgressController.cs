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

    public async Task<IActionResult> Index(int? studentId, int? subjectId)
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
                SubjectId = g.Key.SubjectId,

                SubjectName = g.Key.Name,

                GradesCount = g.Count(),

                AverageGrade = Math.Round(
                    g.Average(x => x.Value),
                    2)
            })
            .OrderBy(s => s.SubjectName)
            .ToList();

        int? comparisonSubjectId = null;
        string? comparisonSubjectName = null;
        var groupComparison = new List<GroupComparisonPoint>();

        if (student.GroupId.HasValue && subjects.Any())
        {
            comparisonSubjectId = subjectId.HasValue &&
                subjects.Any(s => s.SubjectId == subjectId.Value)
                    ? subjectId
                    : subjects
                        .OrderByDescending(s => s.GradesCount)
                        .First()
                        .SubjectId;

            comparisonSubjectName = subjects
                .First(s => s.SubjectId == comparisonSubjectId)
                .SubjectName;

            var groupGrades = await Context.Grades
                .Where(g =>
                    g.SubjectId == comparisonSubjectId.Value &&
                    g.Student.GroupId == student.GroupId)
                .Select(g => new { g.Date, g.Value, g.StudentId })
                .ToListAsync();

            var buckets = groupGrades
                .Select(g => new DateTime(
                    g.Date.Year,
                    g.Date.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            groupComparison = buckets
                .Select(bucket =>
                {
                    var inBucket = groupGrades
                        .Where(g =>
                            g.Date.Year == bucket.Year &&
                            g.Date.Month == bucket.Month)
                        .ToList();

                    var childInBucket = inBucket
                        .Where(g => g.StudentId == student.Id)
                        .ToList();

                    return new GroupComparisonPoint
                    {
                        MonthLabel = bucket.ToString("MMM yyyy"),

                        GroupAverage = inBucket.Any()
                            ? Math.Round(
                                inBucket.Average(g => g.Value),
                                2)
                            : null,

                        ChildAverage = childInBucket.Any()
                            ? Math.Round(
                                childInBucket.Average(g => g.Value),
                                2)
                            : null
                    };
                })
                .ToList();
        }

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

            Subjects = subjects,

            ComparisonSubjectId = comparisonSubjectId,

            ComparisonSubjectName = comparisonSubjectName,

            GroupComparison = groupComparison
        };

        return View(model);
    }
}