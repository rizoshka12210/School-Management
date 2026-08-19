using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
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

            model.Children.Add(new ChildSummary
            {
                StudentId = student.Id,
                FullName = $"{student.FirstName} {student.LastName}",
                GroupName = student.Group?.Name ?? "-",

                AttendanceRate = total == 0
                    ? 0
                    : Math.Round(present * 100.0 / total, 1),

                AverageGrade = grades.Any()
                    ? Math.Round(grades.Average(g => g.Value), 2)
                    : 0,

                RecentGrades = grades
                    .Take(5)
                    .Select(g => new RecentGrade
                    {
                        SubjectName = g.Subject.Name,
                        Value = g.Value,
                        Date = g.Date
                    })
                    .ToList()
            });
        }

        return View(model);
    }
}
