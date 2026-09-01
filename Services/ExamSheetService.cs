using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Builds and saves the two-exam sheet for a group/subject pair
/// (Имтихони №1, Имтихони №2, Балли миёна) - one row per student,
/// mirroring the spreadsheet teachers already keep by hand.
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

        var model = new ExamSheetViewModel
        {
            GroupId = group.Id,
            GroupName = group.Name,
            SubjectId = subject.Id,
            SubjectName = subject.Name
        };

        foreach (var student in students)
        {
            var record = existing.FirstOrDefault(e => e.StudentId == student.Id);

            model.Rows.Add(new ExamSheetRowViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                Exam1 = record?.Exam1,
                Exam2 = record?.Exam2,
                Comment = record?.Comment
            });
        }

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

        foreach (var row in rows)
        {
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            var record = existing.FirstOrDefault(e => e.StudentId == row.StudentId);
            var hasContent = row.Exam1.HasValue ||
                row.Exam2.HasValue ||
                !string.IsNullOrWhiteSpace(row.Comment);

            if (!hasContent)
            {
                if (record != null)
                {
                    _context.ExamGrades.Remove(record);
                }

                continue;
            }

            if (record == null)
            {
                _context.ExamGrades.Add(new ExamGrade
                {
                    StudentId = row.StudentId,
                    SubjectId = subjectId,
                    GroupId = groupId,
                    TeacherId = teacherId,
                    Exam1 = row.Exam1,
                    Exam2 = row.Exam2,
                    Comment = row.Comment,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                record.Exam1 = row.Exam1;
                record.Exam2 = row.Exam2;
                record.Comment = row.Comment;
                record.TeacherId = teacherId;
                record.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }
}
