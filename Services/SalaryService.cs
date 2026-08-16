using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Salary is intentionally not a stored entity. It is derived from
/// Teacher.HourlyRate and the Lessons a teacher actually conducted
/// in a given month, so it can never drift out of sync with the
/// underlying lesson data.
/// </summary>
public class TeacherSalarySummary
{
    public int TeacherId { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Month { get; set; }

    public int LessonsCount { get; set; }

    public decimal WorkedHours { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal TotalSalary { get; set; }
}

public class SalaryService
{
    private readonly AppDbContext _context;

    public SalaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TeacherSalarySummary?> CalculateAsync(
        int teacherId,
        int year,
        int month)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == teacherId);

        if (teacher == null)
        {
            return null;
        }

        var monthStart = new DateTime(
            year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthEnd = monthStart.AddMonths(1);

        var lessons = await _context.Lessons
            .Where(l =>
                l.TeacherId == teacherId &&
                l.StartTime >= monthStart &&
                l.StartTime < monthEnd)
            .Select(l => new { l.StartTime, l.EndTime })
            .ToListAsync();

        var workedHours = lessons
            .Sum(l => (decimal)(l.EndTime - l.StartTime).TotalHours);

        return new TeacherSalarySummary
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.ApplicationUser.FullName,
            Year = year,
            Month = month,
            LessonsCount = lessons.Count,
            WorkedHours = Math.Round(workedHours, 2),
            HourlyRate = teacher.HourlyRate,
            TotalSalary = Math.Round(workedHours * teacher.HourlyRate, 2)
        };
    }

    public async Task<List<TeacherSalarySummary>> CalculateForAllTeachersAsync(
        int year,
        int month)
    {
        var monthStart = new DateTime(
            year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthEnd = monthStart.AddMonths(1);

        var teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();

        var lessons = await _context.Lessons
            .Where(l =>
                l.StartTime >= monthStart &&
                l.StartTime < monthEnd)
            .Select(l => new { l.TeacherId, l.StartTime, l.EndTime })
            .ToListAsync();

        var lessonsByTeacher = lessons
            .GroupBy(l => l.TeacherId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<TeacherSalarySummary>();

        foreach (var teacher in teachers)
        {
            lessonsByTeacher.TryGetValue(
                teacher.Id,
                out var teacherLessons);

            teacherLessons ??= new();

            var workedHours = teacherLessons
                .Sum(l => (decimal)(l.EndTime - l.StartTime).TotalHours);

            results.Add(new TeacherSalarySummary
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.ApplicationUser.FullName,
                Year = year,
                Month = month,
                LessonsCount = teacherLessons.Count,
                WorkedHours = Math.Round(workedHours, 2),
                HourlyRate = teacher.HourlyRate,
                TotalSalary = Math.Round(
                    workedHours * teacher.HourlyRate, 2)
            });
        }

        return results;
    }
}
