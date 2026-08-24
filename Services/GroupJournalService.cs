using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Services;

public class GroupJournalService
{
    private readonly AppDbContext _context;

    public GroupJournalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GroupJournalViewModel?> BuildAsync(
        int groupId,
        int weeks = 8,
        int? teacherId = null)
    {
        weeks = Math.Clamp(weeks, 1, 16);

        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return null;
        }

        var students = await _context.Students
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var lessonsQuery = _context.Lessons
            .Where(l => l.GroupId == groupId);

        if (teacherId.HasValue)
        {
            lessonsQuery = lessonsQuery.Where(l => l.TeacherId == teacherId.Value);
        }

        var allLessons = await lessonsQuery
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .AsSplitQuery()
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        var selectedWeekStarts = allLessons
            .Select(l => StartOfWeek(l.StartTime))
            .Distinct()
            .OrderByDescending(date => date)
            .Take(weeks)
            .ToHashSet();

        var selectedLessons = allLessons
            .Where(l => selectedWeekStarts.Contains(StartOfWeek(l.StartTime)))
            .OrderBy(l => l.StartTime)
            .ThenBy(l => l.Id)
            .ToList();

        var currentWeekStart = StartOfWeek(DateTime.UtcNow);

        var model = new GroupJournalViewModel
        {
            GroupId = group.Id,
            GroupName = group.Name,
            WeeksRequested = weeks,
            TotalStudents = students.Count,
            TotalLessons = selectedLessons.Count
        };

        var allRenderedCells = new List<GroupJournalCellViewModel>();

        foreach (var weekGroup in selectedLessons
                     .GroupBy(l => StartOfWeek(l.StartTime))
                     .OrderByDescending(g => g.Key))
        {
            var weekLessons = weekGroup
                .OrderBy(l => l.StartTime)
                .ThenBy(l => l.Id)
                .ToList();

            var week = new GroupJournalWeekViewModel
            {
                WeekNumber = ISOWeek.GetWeekOfYear(weekGroup.Key),
                StartDate = weekGroup.Key,
                EndDate = weekGroup.Key.AddDays(6),
                IsCurrentWeek = weekGroup.Key.Date == currentWeekStart.Date,
                Lessons = weekLessons
                    .Select(l => new GroupJournalLessonViewModel
                    {
                        LessonId = l.Id,
                        StartTime = l.StartTime,
                        SubjectName = l.Subject.Name,
                        TeacherName = l.Teacher.ApplicationUser.FullName,
                        Topic = l.Topic
                    })
                    .ToList()
            };

            foreach (var student in students)
            {
                var row = new GroupJournalStudentViewModel
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}"
                };

                foreach (var lesson in weekLessons)
                {
                    var attendance = lesson.Attendances
                        .FirstOrDefault(a => a.StudentId == student.Id);

                    var grade = lesson.Grades
                        .Where(g => g.StudentId == student.Id)
                        .OrderByDescending(g => g.Date)
                        .ThenByDescending(g => g.Id)
                        .FirstOrDefault();

                    var cell = new GroupJournalCellViewModel
                    {
                        StudentId = student.Id,
                        LessonId = lesson.Id,
                        AttendanceStatus = attendance?.Status,
                        GradeValue = grade?.Value
                    };

                    row.Cells.Add(cell);
                    allRenderedCells.Add(cell);
                }

                var marked = row.Cells
                    .Where(c => c.AttendanceStatus.HasValue)
                    .ToList();

                var present = marked.Count(c =>
                    c.AttendanceStatus is AttendanceStatus.Present
                        or AttendanceStatus.Late);

                row.AttendanceRate = marked.Count == 0
                    ? 0
                    : Math.Round(present * 100.0 / marked.Count, 1);

                var grades = row.Cells
                    .Where(c => c.GradeValue.HasValue)
                    .Select(c => c.GradeValue!.Value)
                    .ToList();

                row.AverageGrade = grades.Count == 0
                    ? null
                    : Math.Round(grades.Average(), 2);

                week.Students.Add(row);
            }

            model.Weeks.Add(week);
        }

        var markedCells = allRenderedCells
            .Where(c => c.AttendanceStatus.HasValue)
            .ToList();

        var presentCells = markedCells.Count(c =>
            c.AttendanceStatus is AttendanceStatus.Present
                or AttendanceStatus.Late);

        model.MarkedAttendanceCount = markedCells.Count;
        model.AttendanceRate = markedCells.Count == 0
            ? 0
            : Math.Round(presentCells * 100.0 / markedCells.Count, 1);

        var gradeValues = allRenderedCells
            .Where(c => c.GradeValue.HasValue)
            .Select(c => c.GradeValue!.Value)
            .ToList();

        model.GradedEntriesCount = gradeValues.Count;
        model.AverageGrade = gradeValues.Count == 0
            ? null
            : Math.Round(gradeValues.Average(), 2);

        return model;
    }

    public async Task<bool> SaveAsync(
        int groupId,
        IEnumerable<GroupJournalEntryInputModel> entries,
        int? teacherId = null)
    {
        var groupExists = await _context.Groups
            .AnyAsync(g => g.Id == groupId);

        if (!groupExists)
        {
            return false;
        }

        var normalizedEntries = entries
            .Where(e => e.StudentId > 0 && e.LessonId > 0)
            .GroupBy(e => new { e.StudentId, e.LessonId })
            .Select(g => g.Last())
            .ToList();

        if (normalizedEntries.Count == 0)
        {
            return true;
        }

        var studentIds = normalizedEntries
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        var lessonIds = normalizedEntries
            .Select(e => e.LessonId)
            .Distinct()
            .ToList();

        var validStudentIds = (await _context.Students
                .Where(s => s.GroupId == groupId && studentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();

        var lessonsQuery = _context.Lessons
            .Where(l => l.GroupId == groupId && lessonIds.Contains(l.Id));

        if (teacherId.HasValue)
        {
            lessonsQuery = lessonsQuery.Where(l => l.TeacherId == teacherId.Value);
        }

        var lessons = await lessonsQuery
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .AsSplitQuery()
            .ToListAsync();

        var lessonLookup = lessons.ToDictionary(l => l.Id);

        foreach (var entry in normalizedEntries)
        {
            if (!validStudentIds.Contains(entry.StudentId) ||
                !lessonLookup.TryGetValue(entry.LessonId, out var lesson))
            {
                continue;
            }

            var existingAttendance = lesson.Attendances
                .FirstOrDefault(a => a.StudentId == entry.StudentId);

            var status = entry.AttendanceStatus;

            if (status.HasValue &&
                !Enum.IsDefined(typeof(AttendanceStatus), status.Value))
            {
                status = null;
            }

            if (!status.HasValue)
            {
                if (existingAttendance != null)
                {
                    _context.Attendances.Remove(existingAttendance);
                }
            }
            else if (existingAttendance == null)
            {
                _context.Attendances.Add(new Attendance
                {
                    StudentId = entry.StudentId,
                    LessonId = lesson.Id,
                    Status = status.Value
                });
            }
            else
            {
                existingAttendance.Status = status.Value;
            }

            var existingGrades = lesson.Grades
                .Where(g => g.StudentId == entry.StudentId)
                .OrderByDescending(g => g.Date)
                .ThenByDescending(g => g.Id)
                .ToList();

            if (!entry.GradeValue.HasValue)
            {
                if (existingGrades.Count > 0)
                {
                    _context.Grades.RemoveRange(existingGrades);
                }

                continue;
            }

            if (entry.GradeValue.Value is < 1 or > 5)
            {
                continue;
            }

            var primaryGrade = existingGrades.FirstOrDefault();

            if (primaryGrade == null)
            {
                _context.Grades.Add(new Grade
                {
                    StudentId = entry.StudentId,
                    LessonId = lesson.Id,
                    SubjectId = lesson.SubjectId,
                    TeacherId = lesson.TeacherId,
                    Value = entry.GradeValue.Value,
                    Date = EnsureUtc(lesson.StartTime)
                });
            }
            else
            {
                primaryGrade.Value = entry.GradeValue.Value;
                primaryGrade.SubjectId = lesson.SubjectId;
                primaryGrade.TeacherId = lesson.TeacherId;
                primaryGrade.LessonId = lesson.Id;

                if (existingGrades.Count > 1)
                {
                    _context.Grades.RemoveRange(existingGrades.Skip(1));
                }
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var day = date.Date;
        var diff = (7 + (int)day.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return day.AddDays(-diff);
    }

    private static DateTime EnsureUtc(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
