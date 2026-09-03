using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.Services;

public class LeaderboardEntry
{
    public int StudentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public decimal Score { get; set; }
}

/// <summary>
/// Rankings are derived on the fly, same "compute don't store" approach
/// as SalaryService/AchievementService. A minimum sample size keeps a
/// single lucky grade or attendance record from topping the board.
/// Only ever returns a top-N list - there is deliberately no "bottom of
/// the class" query anywhere in this service.
/// </summary>
public class LeaderboardService
{
    private const int MinSampleSize = 3;

    private readonly AppDbContext _context;

    public LeaderboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaderboardEntry>> GetTopByGradeAsync(
        int year, int month, int take = 10)
    {
        var (start, end) = MonthRange(year, month);

        var grades = await _context.Grades
            .Where(g => g.Date >= start && g.Date < end)
            .Include(g => g.Student)
                .ThenInclude(s => s.Group)
            .ToListAsync();

        var examGrades = GradeAveragingHelper.LatestPerStudentSubject(
            await _context.ExamGrades
                .Where(e => e.UpdatedAt >= start && e.UpdatedAt < end)
                .Include(e => e.Student)
                    .ThenInclude(s => s.Group)
                .ToListAsync());

        var studentIds = grades.Select(g => g.StudentId)
            .Union(examGrades.Select(e => e.StudentId))
            .Distinct();

        var entries = new List<LeaderboardEntry>();

        foreach (var studentId in studentIds)
        {
            var studentGrades = grades
                .Where(g => g.StudentId == studentId)
                .ToList();

            var studentExamGrades = examGrades
                .Where(e => e.StudentId == studentId)
                .ToList();

            var sampleSize = studentGrades.Count +
                studentExamGrades.Count(e => e.Average.HasValue);

            if (sampleSize < MinSampleSize)
            {
                continue;
            }

            var average = GradeAveragingHelper.Combine(
                studentGrades.Select(g => g.Value),
                studentExamGrades.Select(e => e.Average));

            if (average == null)
            {
                continue;
            }

            var student = studentGrades.FirstOrDefault()?.Student
                ?? studentExamGrades.First().Student;

            entries.Add(new LeaderboardEntry
            {
                StudentId = studentId,
                Name = FullName(student),
                GroupName = student.Group?.Name ?? "-",
                Score = average.Value
            });
        }

        return entries
            .OrderByDescending(e => e.Score)
            .Take(take)
            .ToList();
    }

    public async Task<List<LeaderboardEntry>> GetTopByAttendanceAsync(
        int year, int month, int take = 10)
    {
        var (start, end) = MonthRange(year, month);

        var attendances = await _context.Attendances
            .Include(a => a.Lesson)
            .Include(a => a.Student)
                .ThenInclude(s => s.Group)
            .Where(a =>
                a.Lesson.StartTime >= start &&
                a.Lesson.StartTime < end)
            .ToListAsync();

        return attendances
            .GroupBy(a => a.StudentId)
            .Where(g => g.Count() >= MinSampleSize)
            .Select(g => new LeaderboardEntry
            {
                StudentId = g.Key,
                Name = FullName(g.First().Student),
                GroupName = g.First().Student.Group?.Name ?? "-",
                Score = Math.Round(
                    (decimal)g.Count(a =>
                        a.Status is AttendanceStatus.Present
                            or AttendanceStatus.Late) * 100m / g.Count(),
                    1)
            })
            .OrderByDescending(e => e.Score)
            .Take(take)
            .ToList();
    }

    public async Task<List<LeaderboardEntry>> GetMostImprovedAsync(
        int year, int month, int take = 10)
    {
        var (currentStart, currentEnd) = MonthRange(year, month);
        var previousStart = currentStart.AddMonths(-1);

        var relevantGrades = await _context.Grades
            .Where(g => g.Date >= previousStart && g.Date < currentEnd)
            .Include(g => g.Student)
                .ThenInclude(s => s.Group)
            .ToListAsync();

        var entries = new List<LeaderboardEntry>();

        foreach (var studentGroup in relevantGrades.GroupBy(g => g.StudentId))
        {
            var current = studentGroup
                .Where(g => g.Date >= currentStart && g.Date < currentEnd)
                .ToList();

            var previous = studentGroup
                .Where(g => g.Date >= previousStart && g.Date < currentStart)
                .ToList();

            if (current.Count < MinSampleSize ||
                previous.Count < MinSampleSize)
            {
                continue;
            }

            var improvement = (decimal)current.Average(g => g.Value) -
                (decimal)previous.Average(g => g.Value);

            if (improvement <= 0)
            {
                continue;
            }

            var student = studentGroup.First().Student;

            entries.Add(new LeaderboardEntry
            {
                StudentId = studentGroup.Key,
                Name = FullName(student),
                GroupName = student.Group?.Name ?? "-",
                Score = Math.Round(improvement, 2)
            });
        }

        return entries
            .OrderByDescending(e => e.Score)
            .Take(take)
            .ToList();
    }

    private static (DateTime Start, DateTime End) MonthRange(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        return (start, start.AddMonths(1));
    }

    private static string FullName(Models.Entities.Student student)
    {
        return $"{student.FirstName} {student.LastName}";
    }
}
