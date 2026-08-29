using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly StudentRiskService _riskService;

    public DashboardController(
        AppDbContext context,
        StudentRiskService riskService)
    {
        _context = context;
        _riskService = riskService;
    }

    public async Task<IActionResult> Index(string period = "week")
    {
        period = period.Equals("month", StringComparison.OrdinalIgnoreCase)
            ? "month"
            : "week";

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var trendStart = period == "month"
            ? today.AddDays(-29)
            : today.AddDays(-6);

        var todayLessons = await _context.Lessons
            .Where(l => l.StartTime >= today && l.StartTime < tomorrow)
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

        var students = await _context.Students
            .Include(s => s.Group)
            .Include(s => s.Grades)
            .Include(s => s.Attendances)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();

        var performance = students
            .Select(student =>
            {
                var risk = _riskService.Evaluate(student);

                return new StudentPerformanceViewModel
                {
                    StudentId = student.Id,
                    FullName = $"{student.FirstName} {student.LastName}",
                    GroupName = student.Group?.Name ?? "No group",
                    AverageGrade = risk.AverageGrade,
                    AttendanceRate = risk.AttendanceRate,
                    RiskStatus = risk.Status,
                    RiskLabel = risk.Label,
                    RiskCssClass = risk.CssClass,
                    RiskIcon = risk.Icon,
                    RiskReasons = risk.Reasons.ToList()
                };
            })
            .ToList();

        var allGrades = await _context.Grades
            .Select(g => g.Value)
            .ToListAsync();

        var allAttendance = await _context.Attendances
            .Select(a => a.Status)
            .ToListAsync();

        var trendRecords = await _context.Attendances
            .Where(a => a.Lesson.StartTime >= trendStart &&
                        a.Lesson.StartTime < tomorrow)
            .Include(a => a.Lesson)
            .ToListAsync();

        var trend = Enumerable.Range(0, (today - trendStart).Days + 1)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var dayRecords = trendRecords
                    .Where(a => a.Lesson.StartTime.Date == date.Date)
                    .ToList();

                var total = dayRecords.Count;
                var absent = dayRecords.Count(a => a.Status == AttendanceStatus.Absent);
                var rate = total == 0
                    ? 0
                    : (total - absent) * 100.0 / total;

                return new AttendanceTrendPointViewModel
                {
                    Label = period == "month"
                        ? date.ToString("dd MMM")
                        : date.ToString("ddd"),
                    FullLabel = date.ToString("dd MMM"),
                    Rate = rate,
                    Total = total,
                    Absent = absent
                };
            })
            .ToList();

        var model = new AdminDashboardViewModel
        {
            StudentsCount = students.Count,
            TeachersCount = await _context.Teachers.CountAsync(),
            GroupsCount = await _context.Groups.CountAsync(),
            SubjectsCount = await _context.Subjects.CountAsync(),
            TodayLessonsCount = todayLessons.Count,
            AverageGrade = allGrades.Count == 0 ? 0 : allGrades.Average(),
            AttendanceRate = allAttendance.Count == 0
                ? 0
                : allAttendance.Count(a => a != AttendanceStatus.Absent) * 100.0 / allAttendance.Count,
            StudentsAbsentToday = await _context.Attendances
                .Where(a => a.Lesson.StartTime >= today &&
                            a.Lesson.StartTime < tomorrow &&
                            a.Status == AttendanceStatus.Absent)
                .Select(a => a.StudentId)
                .Distinct()
                .CountAsync(),
            TrendPeriod = period,
            TodayLessons = todayLessons,
            RecentStudents = recentStudents,
            TopStudents = performance
                .Where(p => p.AverageGrade > 0)
                .OrderByDescending(p => p.AverageGrade)
                .ThenByDescending(p => p.AttendanceRate)
                .Take(5)
                .ToList(),
            StudentsNeedingAttention = performance
                .Where(p => p.RiskStatus != StudentRiskStatus.Good)
                .OrderByDescending(p => p.RiskStatus == StudentRiskStatus.AtRisk)
                .ThenBy(p => p.AverageGrade == 0 ? double.MaxValue : p.AverageGrade)
                .ThenBy(p => p.AttendanceRate)
                .Take(8)
                .ToList(),
            AttendanceTrend = trend,
            UpcomingEvents = await _context.CalendarEvents
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }
}
