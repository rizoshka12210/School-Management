using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Builds and saves the two-exam sheet for a group/subject pair
/// (Имтихони №1, Имтихони №2, Балли миёна) - one row per student,
/// mirroring the spreadsheet teachers already keep by hand.
///
/// ExamGrade is append-only: saving never overwrites a student's
/// previous result, it adds a new row only when something actually
/// changed, so the exam history is never lost - the sheet always
/// shows the latest row per student as the "current" value.
/// </summary>
public class ExamSheetService
{
    private readonly AppDbContext _context;

    public ExamSheetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExamSheetViewModel?> BuildAsync(int groupId, int subjectId)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var subject = await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subjectId);

        if (group == null || subject == null)
        {
            return null;
        }

        var students = await _context.Students
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var existing = await _context.ExamGrades
            .AsNoTracking()
            .Where(e => e.GroupId == groupId && e.SubjectId == subjectId)
            .ToListAsync();

        var latestByStudent = GradeAveragingHelper
            .LatestPerStudentSubject(existing)
            .ToDictionary(e => e.StudentId);

        var currentIds = latestByStudent.Values.Select(e => e.Id).ToHashSet();

        var studentNames = students.ToDictionary(
            s => s.Id,
            s => $"{s.FirstName} {s.LastName}");

        var threshold = await _context.ExamBlacklistThresholds
            .AsNoTracking()
            .Where(t => t.GroupId == groupId && t.SubjectId == subjectId)
            .Select(t => (decimal?)t.Threshold)
            .FirstOrDefaultAsync();

        var model = new ExamSheetViewModel
        {
            GroupId = group.Id,
            GroupName = group.Name,
            SubjectId = subject.Id,
            SubjectName = subject.Name,
            BlacklistThreshold = threshold
        };

        foreach (var student in students)
        {
            latestByStudent.TryGetValue(student.Id, out var record);

            model.Rows.Add(new ExamSheetRowViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                Exam1 = record?.Exam1,
                Exam2 = record?.Exam2,
                Comment = record?.Comment
            });
        }

        model.History = existing
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => new ExamGradeHistoryEntryViewModel
            {
                StudentName = studentNames.TryGetValue(e.StudentId, out var name) ? name : "—",
                Exam1 = e.Exam1,
                Exam2 = e.Exam2,
                Comment = e.Comment,
                UpdatedAt = e.UpdatedAt,
                IsCurrent = currentIds.Contains(e.Id)
            })
            .ToList();

        return model;
    }

    public async Task<bool> SaveAsync(
        int groupId,
        int subjectId,
        int teacherId,
        IEnumerable<ExamSheetRowViewModel> rows)
    {
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == subjectId);

        if (!groupExists || !subjectExists)
        {
            return false;
        }

        var validStudentIds = (await _context.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();

        var existing = await _context.ExamGrades
            .Where(e => e.GroupId == groupId && e.SubjectId == subjectId)
            .ToListAsync();

        var latestByStudent = GradeAveragingHelper
            .LatestPerStudentSubject(existing)
            .ToDictionary(e => e.StudentId);

        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            var hasContent = row.Exam1.HasValue ||
                row.Exam2.HasValue ||
                !string.IsNullOrWhiteSpace(row.Comment);

            if (!hasContent)
            {
                // Nothing entered - leave whatever history already exists
                // untouched rather than erasing the student's last result.
                continue;
            }

            latestByStudent.TryGetValue(row.StudentId, out var latest);

            var comment = string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment;

            var unchanged = latest != null &&
                latest.Exam1 == row.Exam1 &&
                latest.Exam2 == row.Exam2 &&
                latest.Comment == comment;

            if (unchanged)
            {
                continue;
            }

            _context.ExamGrades.Add(new ExamGrade
            {
                StudentId = row.StudentId,
                SubjectId = subjectId,
                GroupId = groupId,
                TeacherId = teacherId,
                Exam1 = row.Exam1,
                Exam2 = row.Exam2,
                Comment = comment,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetBlacklistThresholdAsync(int groupId, int subjectId, decimal? threshold)
    {
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == subjectId);

        if (!groupExists || !subjectExists)
        {
            return false;
        }

        var existing = await _context.ExamBlacklistThresholds
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.SubjectId == subjectId);

        if (!threshold.HasValue)
        {
            if (existing != null)
            {
                _context.ExamBlacklistThresholds.Remove(existing);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        if (existing == null)
        {
            _context.ExamBlacklistThresholds.Add(new ExamBlacklistThreshold
            {
                GroupId = groupId,
                SubjectId = subjectId,
                Threshold = threshold.Value
            });
        }
        else
        {
            existing.Threshold = threshold.Value;
        }

        await _context.SaveChangesAsync();

        return true;
    }
}
