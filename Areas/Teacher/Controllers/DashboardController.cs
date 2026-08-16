using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Teacher;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class DashboardController : TeacherControllerBase
{
    private readonly SalaryService _salaryService;

    public DashboardController(
        AppDbContext context,
        OwnershipHelper ownership,
        SalaryService salaryService)
        : base(context, ownership)
    {
        _salaryService = salaryService;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var teacher = await Context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstAsync(t => t.Id == teacherId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayLessons = await Context.Lessons
            .Where(l =>
                l.TeacherId == teacherId &&
                l.StartTime >= today &&
                l.StartTime < tomorrow)
            .OrderBy(l => l.StartTime)
            .Select(l => new TeacherLessonSummary
            {
                LessonId = l.Id,
                GroupName = l.Group.Name,
                SubjectName = l.Subject.Name,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Topic = l.Topic,
                AttendanceMarked = l.Attendances.Any()
            })
            .ToListAsync();

        var groupsCount = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .CountAsync();

        var studentsCount = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .SelectMany(g => g.Students)
            .Select(s => s.Id)
            .Distinct()
            .CountAsync();

        var now = DateTime.UtcNow;

        var salary = await _salaryService.CalculateAsync(
            teacherId.Value,
            now.Year,
            now.Month);

        var model = new TeacherDashboardViewModel
        {
            TeacherName = teacher.ApplicationUser.FullName,
            GroupsCount = groupsCount,
            StudentsCount = studentsCount,
            TodayLessons = todayLessons,
            CurrentMonthWorkedHours = salary?.WorkedHours ?? 0,
            CurrentMonthSalary = salary?.TotalSalary ?? 0
        };

        return View(model);
    }
}
