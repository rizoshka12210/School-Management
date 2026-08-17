using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayLessons = await _context.Lessons
            .Where(l =>
                l.StartTime >= today &&
                l.StartTime < tomorrow)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .OrderBy(l => l.StartTime)
            .ToListAsync();

        var recentStudents = await _context.Students
            .Include(s => s.Group)
            .OrderByDescending(s => s.Id)
            .Take(5)
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            StudentsCount =
                await _context.Students.CountAsync(),

            TeachersCount =
                await _context.Teachers.CountAsync(),

            GroupsCount =
                await _context.Groups.CountAsync(),

            SubjectsCount =
                await _context.Subjects.CountAsync(),

            TodayLessonsCount =
                todayLessons.Count,

            TodayLessons =
                todayLessons,

            RecentStudents =
                recentStudents
        };

        return View(model);
    }
}