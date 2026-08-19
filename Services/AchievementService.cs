using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.Services;

public class AchievementBadge
{
    public string Icon { get; set; } = string.Empty;

    public string NameKey { get; set; } = string.Empty;

    public string DescriptionKey { get; set; } = string.Empty;

    public bool Earned { get; set; }
}

/// <summary>
/// Achievements are never stored - like SalaryService, they are derived
/// on the fly from existing Grade/Attendance data each time a student's
/// profile is viewed, so they can never drift out of sync with the
/// underlying records.
/// </summary>
public class AchievementService
{
    private readonly AppDbContext _context;

    public AchievementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AchievementBadge>> GetBadgesAsync(int studentId)
    {
        var grades = await _context.Grades
            .Where(g => g.StudentId == studentId)
            .Select(g => new { g.Value, SubjectName = g.Subject.Name })
            .ToListAsync();

        var attendances = await _context.Attendances
            .Where(a => a.StudentId == studentId)
            .Select(a => new { a.Status, LessonStart = a.Lesson.StartTime })
            .ToListAsync();

        var averageGrade = grades.Any()
            ? grades.Average(g => g.Value)
            : 0;

        var excellentGradesCount = grades.Count(g => g.Value == 5);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var recentAttendances = attendances
            .Where(a => a.LessonStart >= thirtyDaysAgo)
            .ToList();

        var thirtyDayStreak = recentAttendances.Any() &&
            recentAttendances.All(a => a.Status != AttendanceStatus.Absent);

        var mathGrades = grades
            .Where(g => g.SubjectName == "Mathematics")
            .ToList();

        var mathMaster = mathGrades.Any() &&
            mathGrades.Average(g => g.Value) >= 4.5;

        var perfectAttendance = attendances.Any() &&
            attendances.All(a => a.Status != AttendanceStatus.Absent);

        return new List<AchievementBadge>
        {
            new()
            {
                Icon = "🏆",
                NameKey = "Excellent Student",
                DescriptionKey = "Average grade of 4.5 or higher",
                Earned = grades.Any() && averageGrade >= 4.5
            },
            new()
            {
                Icon = "⭐️",
                NameKey = "10 Excellent Grades",
                DescriptionKey = "Earned ten grades of 5",
                Earned = excellentGradesCount >= 10
            },
            new()
            {
                Icon = "🔥",
                NameKey = "30 Days Attendance Streak",
                DescriptionKey = "No absences in the last 30 days",
                Earned = thirtyDayStreak
            },
            new()
            {
                Icon = "📚",
                NameKey = "Mathematics Master",
                DescriptionKey = "Average grade of 4.5 or higher in Mathematics",
                Earned = mathMaster
            },
            new()
            {
                Icon = "🎯",
                NameKey = "100% Attendance",
                DescriptionKey = "Never marked absent",
                Earned = perfectAttendance
            }
        };
    }
}
