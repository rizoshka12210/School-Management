using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Services;

public class AttendanceService
{
    private readonly AppDbContext _context;

    public AttendanceService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Builds a students x lesson-dates pivot for a group. Authorization
    /// (is this admin/teacher allowed to see this group) is the caller's
    /// responsibility - this method has no notion of "who is asking".
    /// </summary>
    public async Task<AttendanceJournalViewModel> BuildJournalAsync(
        int groupId,
        DateTime fromUtc,
        DateTime toUtc,
        int? subjectId = null,
        int? teacherId = null)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var lessonsQuery = _context.Lessons
            .Where(l =>
                l.GroupId == groupId &&
                l.StartTime >= fromUtc &&
                l.StartTime < toUtc);

        if (subjectId.HasValue)
        {
            lessonsQuery = lessonsQuery.Where(
                l => l.SubjectId == subjectId.Value);
        }

        if (teacherId.HasValue)
        {
            lessonsQuery = lessonsQuery.Where(
                l => l.TeacherId == teacherId.Value);
        }

        var lessons = await lessonsQuery
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .OrderBy(l => l.StartTime)
            .ToListAsync();

        var students = await _context.Students
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var cellLookup = new Dictionary<(int LessonId, int StudentId), Attendance>();

        foreach (var lesson in lessons)
        {
            foreach (var attendance in lesson.Attendances)
            {
                cellLookup[(lesson.Id, attendance.StudentId)] = attendance;
            }
        }

        var columns = lessons
            .Select(l => new JournalColumn
            {
                LessonId = l.Id,
                Date = l.StartTime,
                SubjectName = l.Subject.Name,
                Topic = l.Topic
            })
            .ToList();

        var rows = students
            .Select(s =>
            {
                var cells = lessons
                    .Select(l =>
                    {
                        cellLookup.TryGetValue(
                            (l.Id, s.Id),
                            out var attendance);

                        return new JournalCell
                        {
                            AttendanceId = attendance?.Id,
                            Status = attendance?.Status
                        };
                    })
                    .ToList();

                var markedCount = cells.Count(c => c.Status.HasValue);

                var presentCount = cells.Count(c =>
                    c.Status is AttendanceStatus.Present
                        or AttendanceStatus.Late);

                return new JournalRow
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    Cells = cells,
                    AttendanceRate = markedCount == 0
                        ? 0
                        : Math.Round(
                            presentCount * 100.0 / markedCount,
                            1)
                };
            })
            .ToList();

        return new AttendanceJournalViewModel
        {
            GroupId = groupId,
            GroupName = group?.Name ?? string.Empty,
            SubjectName = subjectId.HasValue
                ? lessons.FirstOrDefault()?.Subject.Name
                : null,
            From = fromUtc,
            To = toUtc,
            Columns = columns,
            Rows = rows
        };
    }

    public async Task<List<Attendance>> GetAllAsync()
    {
        return await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Teacher)
                    .ThenInclude(t => t.ApplicationUser)
            .OrderByDescending(a => a.Lesson.StartTime)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetByStudentAsync(
        int studentId)
    {
        return await _context.Attendances
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .OrderByDescending(a => a.Lesson.StartTime)
            .ToListAsync();
    }
}