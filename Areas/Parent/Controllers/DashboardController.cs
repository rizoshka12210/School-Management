using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Parent;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class DashboardController : ParentControllerBase
{
    public DashboardController(
        AppDbContext context,
        OwnershipHelper ownership)
        : base(context, ownership)
    {
    }

    public async Task<IActionResult> Index()
    {
        var parentId = await GetParentIdAsync();

        if (parentId == null)
        {
            return Forbid();
        }

        var students = await Context.Parents
            .Where(p => p.Id == parentId)
            .SelectMany(p => p.Students)
            .Include(s => s.Group)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var sevenDaysAgo = today.AddDays(-7);

        var model = new ParentDashboardViewModel
        {
            UpcomingEvents = await Context.CalendarEvents
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date)
                .Take(5)
                .ToListAsync()
        };

        foreach (var student in students)
        {
            var attendances = await Context.Attendances
                .Include(a => a.Lesson)
                    .ThenInclude(l => l.Subject)
                .Where(a => a.StudentId == student.Id)
                .ToListAsync();

            var total = attendances.Count;

            var present = attendances
                .Count(a => a.Status == AttendanceStatus.Present);

            var grades = await Context.Grades
                .Where(g => g.StudentId == student.Id)
                .Include(g => g.Subject)
                .OrderByDescending(g => g.Date)
                .ToListAsync();

            var examGrades = await Context.ExamGrades
                .Where(e => e.StudentId == student.Id)
                .ToListAsync();

            var lessonsTodayCount = student.GroupId == null
                ? 0
                : await Context.Lessons
                    .Where(l =>
                        l.GroupId == student.GroupId &&
                        l.StartTime >= today &&
                        l.StartTime < tomorrow)
                    .CountAsync();

            var recentMissed = attendances
                .Where(a =>
                    a.Status == AttendanceStatus.Absent &&
                    a.Lesson.StartTime >= sevenDaysAgo)
                .OrderByDescending(a => a.Lesson.StartTime)
                .Select(a => new MissedLesson
                {
                    SubjectName = a.Lesson.Subject.Name,
                    Date = a.Lesson.StartTime
                })
                .FirstOrDefault();

            model.Children.Add(new ChildSummary
            {
                StudentId = student.Id,
                FullName = $"{student.FirstName} {student.LastName}",
                GroupName = student.Group?.Name ?? "-",

                AttendanceRate = total == 0
                    ? 0
                    : Math.Round(present * 100.0 / total, 1),

                AverageGrade = (double)(GradeAveragingHelper.Combine(
                    grades.Select(g => g.Value),
                    examGrades.Select(e => e.Average)) ?? 0),

                RecentGrades = grades
                    .Take(5)
                    .Select(g => new RecentGrade
                    {
                        SubjectName = g.Subject.Name,
                        Value = g.Value,
                        Date = g.Date
                    })
                    .ToList(),

                LessonsTodayCount = lessonsTodayCount,
                RecentMissedLesson = recentMissed
            });
        }

        return View(model);
    }
}
