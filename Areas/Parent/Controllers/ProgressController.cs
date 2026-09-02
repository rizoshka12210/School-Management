using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
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

        var examGrades = await Context.ExamGrades
            .Where(e => e.StudentId == student.Id)
            .Include(e => e.Subject)
            .ToListAsync();

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

        var subjectIds = grades.Select(g => g.SubjectId)
            .Union(examGrades.Select(e => e.SubjectId))
            .Distinct();

        var subjects = subjectIds
            .Select(subjectId =>
            {
                var subjectGrades = grades
                    .Where(g => g.SubjectId == subjectId)
                    .ToList();

                var exam = examGrades
                    .FirstOrDefault(e => e.SubjectId == subjectId);

                var subjectName = subjectGrades.FirstOrDefault()?.Subject.Name
                    ?? exam?.Subject.Name
                    ?? string.Empty;

                var average = GradeAveragingHelper.Combine(
                    subjectGrades.Select(g => g.Value),
                    exam == null ? Array.Empty<decimal?>() : new[] { exam.Average });

                return new SubjectProgressViewModel
                {
                    SubjectId = subjectId,
                    SubjectName = subjectName,
                    GradesCount = subjectGrades.Count,
                    AverageGrade = (double)(average ?? 0)
                };
            })
            .OrderBy(s => s.SubjectName)
            .ToList();

        int? comparisonSubjectId = null;
        string? comparisonSubjectName = null;
        GroupComparisonChartViewModel? groupComparison = null;

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
                .Select(g => new
                {
                    g.Date,
                    g.Value,
                    g.StudentId,
                    g.Student.FirstName,
                    g.Student.LastName
                })
                .ToListAsync();

            var buckets = groupGrades
                .Select(g => g.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // One line per student in the group (not a blended average),
            // so the parent can see exactly where their child stands
            // among their actual classmates - the child's own line is
            // flagged via IsChild for the view to highlight it. Points
            // are per grading date rather than per month, so the chart
            // is as dense as the group's actual grade history.
            var series = groupGrades
                .GroupBy(g => new { g.StudentId, g.FirstName, g.LastName })
                .Select(g => new StudentSeriesViewModel
                {
                    StudentId = g.Key.StudentId,
                    StudentName = $"{g.Key.FirstName} {g.Key.LastName}",
                    IsChild = g.Key.StudentId == student.Id,
                    Values = buckets
                        .Select(bucket =>
                        {
                            var onDate = g
                                .Where(x => x.Date.Date == bucket)
                                .ToList();

                            return onDate.Any()
                                ? (double?)Math.Round(onDate.Average(x => x.Value), 2)
                                : null;
                        })
                        .ToList()
                })
                .OrderByDescending(s => s.IsChild)
                .ThenBy(s => s.StudentName)
                .ToList();

            groupComparison = new GroupComparisonChartViewModel
            {
                PointLabels = buckets.Select(b => b.ToString("dd MMM")).ToList(),
                Series = series
            };
        }

        var model = new ProgressViewModel
        {
            StudentId = student.Id,

            StudentName =
                $"{student.FirstName} {student.LastName}",

            GroupName = student.Group?.Name,

            GradesCount = grades.Count,

            AverageGrade = (double)(GradeAveragingHelper.Combine(
                grades.Select(g => g.Value),
                examGrades.Select(e => e.Average)) ?? 0),

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