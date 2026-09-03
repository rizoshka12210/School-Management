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

        var examGradeEntities = await _context.ExamGrades
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Subject)
            .ToListAsync();

        var examGrades = GradeAveragingHelper
            .LatestPerStudentSubject(examGradeEntities)
            .Select(e => new { e.Average, SubjectName = e.Subject.Name })
            .ToList();

        var attendances = await _context.Attendances
            .Where(a => a.StudentId == studentId)
            .Select(a => new { a.Status, LessonStart = a.Lesson.StartTime })
            .ToListAsync();

        var averageGrade = GradeAveragingHelper.Combine(
            grades.Select(g => g.Value),
            examGrades.Select(e => e.Average)) ?? 0;

        var excellentGradesCount = grades.Count(g => g.Value >= 90) +
            examGrades.Count(e => e.Average >= 90);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var recentAttendances = attendances
            .Where(a => a.LessonStart >= thirtyDaysAgo)
            .ToList();

        var thirtyDayStreak = recentAttendances.Any() &&
            recentAttendances.All(a => a.Status != AttendanceStatus.Absent);

        var mathGrades = grades
            .Where(g => g.SubjectName == "Mathematics")
            .Select(g => g.Value);

        var mathExamGrades = examGrades
            .Where(e => e.SubjectName == "Mathematics")
            .Select(e => e.Average);

        var mathAverage = GradeAveragingHelper.Combine(mathGrades, mathExamGrades);

        var mathMaster = mathAverage >= 90;

        var perfectAttendance = attendances.Any() &&
            attendances.All(a => a.Status != AttendanceStatus.Absent);

        return new List<AchievementBadge>
        {
            new()
            {
                Icon = "bi-trophy-fill",
                NameKey = "Excellent Student",
                DescriptionKey = "Average grade of 90 or higher",
                Earned = (grades.Any() || examGrades.Any()) && averageGrade >= 90
            },
            new()
            {
                Icon = "bi-star-fill",
                NameKey = "10 Excellent Grades",
                DescriptionKey = "Earned ten grades of 90 or higher",
                Earned = excellentGradesCount >= 10
            },
            new()
            {
                Icon = "bi-fire",
                NameKey = "30 Days Attendance Streak",
                DescriptionKey = "No absences in the last 30 days",
                Earned = thirtyDayStreak
            },
            new()
            {
                Icon = "bi-book-fill",
                NameKey = "Mathematics Master",
                DescriptionKey = "Average grade of 90 or higher in Mathematics",
                Earned = mathMaster
            },
            new()
            {
                Icon = "bi-bullseye",
                NameKey = "100% Attendance",
                DescriptionKey = "Never marked absent",
                Earned = perfectAttendance
            }
        };
    }
}
