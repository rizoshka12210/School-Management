using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Services;

public class BigExamRankingEntry
{
    public int BigExamId { get; set; }

    public string BigExamTitle { get; set; } = string.Empty;

    public DateTime BigExamDate { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public int GroupRank { get; set; }

    public int GroupSize { get; set; }

    public int OverallRank { get; set; }

    public int OverallSize { get; set; }
}

/// <summary>
/// Manages the periodic school-wide Big Exam: exam sessions, the
/// append-only grade sheet per group (mirrors ExamSheetService), and
/// the group/overall rankings derived from the latest score per
/// student. Only Admin and the single designated teacher
/// (Teacher.IsBigExamGrader) are ever allowed to call the write methods
/// here - that check happens in the calling controllers.
/// </summary>
public class BigExamService
{
    private readonly AppDbContext _context;

    public BigExamService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<BigExam>> ListAsync()
    {
        return _context.BigExams
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public Task<BigExam?> GetAsync(int id)
    {
        return _context.BigExams.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<BigExam> CreateAsync(string title, DateTime date)
    {
        var exam = new BigExam
        {
            Title = title.Trim(),
            Date = DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        _context.BigExams.Add(exam);
        await _context.SaveChangesAsync();

        return exam;
    }

    public async Task<bool> UpdateAsync(int id, string title, DateTime date)
    {
        var exam = await _context.BigExams.FindAsync(id);

        if (exam == null)
        {
            return false;
        }

        exam.Title = title.Trim();
        exam.Date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var exam = await _context.BigExams.FindAsync(id);

        if (exam == null)
        {
            return false;
        }

        _context.BigExams.Remove(exam);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>Collapses append-only history down to the latest row per student.</summary>
    public static List<BigExamGrade> LatestPerStudent(IEnumerable<BigExamGrade> grades)
    {
        return grades
            .GroupBy(g => g.StudentId)
            .Select(g => g
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .First())
            .ToList();
    }

    public async Task<BigExamSheetViewModel?> BuildSheetAsync(int examId, int groupId)
    {
        var exam = await _context.BigExams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);

        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (exam == null || group == null)
        {
            return null;
        }

        var students = await _context.Students
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var history = await _context.BigExamGrades
            .AsNoTracking()
            .Where(g => g.BigExamId == examId && g.GroupId == groupId)
            .Include(g => g.Teacher)
                .ThenInclude(t => t!.ApplicationUser)
            .ToListAsync();

        var latestByStudent = LatestPerStudent(history).ToDictionary(g => g.StudentId);
        var currentIds = latestByStudent.Values.Select(g => g.Id).ToHashSet();

        var studentNames = students.ToDictionary(
            s => s.Id,
            s => $"{s.FirstName} {s.LastName}");

        var model = new BigExamSheetViewModel
        {
            BigExamId = exam.Id,
            BigExamTitle = exam.Title,
            GroupId = group.Id,
            GroupName = group.Name
        };

        foreach (var student in students)
        {
            latestByStudent.TryGetValue(student.Id, out var record);

            model.Rows.Add(new BigExamSheetRowViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                Score = record?.Score,
                Comment = record?.Comment
            });
        }

        model.History = history
            .OrderByDescending(g => g.UpdatedAt)
            .ThenByDescending(g => g.Id)
            .Select(g => new BigExamHistoryEntryViewModel
            {
                StudentName = studentNames.TryGetValue(g.StudentId, out var name) ? name : "—",
                Score = g.Score,
                Comment = g.Comment,
                GradedBy = g.Teacher?.ApplicationUser.FullName ?? "Admin",
                UpdatedAt = g.UpdatedAt,
                IsCurrent = currentIds.Contains(g.Id)
            })
            .ToList();

        return model;
    }

    public async Task<bool> SaveSheetAsync(
        int examId,
        int groupId,
        int? teacherId,
        IEnumerable<BigExamSheetRowViewModel> rows)
    {
        var examExists = await _context.BigExams.AnyAsync(e => e.Id == examId);
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);

        if (!examExists || !groupExists)
        {
            return false;
        }

        var validStudentIds = (await _context.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();

        var existing = await _context.BigExamGrades
            .Where(g => g.BigExamId == examId && g.GroupId == groupId)
            .ToListAsync();

        var latestByStudent = LatestPerStudent(existing).ToDictionary(g => g.StudentId);

        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            var hasContent = row.Score.HasValue || !string.IsNullOrWhiteSpace(row.Comment);

            if (!hasContent)
            {
                // Nothing entered - leave whatever history already
                // exists untouched rather than erasing it.
                continue;
            }

            latestByStudent.TryGetValue(row.StudentId, out var latest);

            var comment = string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment;

            var unchanged = latest != null &&
                latest.Score == row.Score &&
                latest.Comment == comment;

            if (unchanged)
            {
                continue;
            }

            _context.BigExamGrades.Add(new BigExamGrade
            {
                BigExamId = examId,
                StudentId = row.StudentId,
                GroupId = groupId,
                TeacherId = teacherId,
                Score = row.Score,
                Comment = comment,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Group and overall rankings for one Big Exam, using competition
    /// ranking (ties share a rank, the next rank skips the tied count).
    /// </summary>
    public async Task<List<BigExamRankingEntry>> GetRankingsAsync(int examId)
    {
        var exam = await _context.BigExams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
        {
            return new List<BigExamRankingEntry>();
        }

        var history = await _context.BigExamGrades
            .Where(g => g.BigExamId == examId)
            .Include(g => g.Student)
            .Include(g => g.Group)
            .ToListAsync();

        var latest = LatestPerStudent(history)
            .Where(g => g.Score.HasValue)
            .ToList();

        var overallSize = latest.Count;

        var entries = new List<BigExamRankingEntry>();

        foreach (var group in latest.GroupBy(g => g.GroupId))
        {
            var groupSize = group.Count();

            foreach (var g in group)
            {
                var overallRank = 1 + latest.Count(o => o.Score!.Value > g.Score!.Value);
                var groupRank = 1 + group.Count(o => o.Score!.Value > g.Score!.Value);

                entries.Add(new BigExamRankingEntry
                {
                    BigExamId = exam.Id,
                    BigExamTitle = exam.Title,
                    BigExamDate = exam.Date,
                    StudentId = g.StudentId,
                    StudentName = $"{g.Student.FirstName} {g.Student.LastName}",
                    GroupId = g.GroupId,
                    GroupName = g.Group.Name,
                    Score = g.Score!.Value,
                    GroupRank = groupRank,
                    GroupSize = groupSize,
                    OverallRank = overallRank,
                    OverallSize = overallSize
                });
            }
        }

        return entries
            .OrderBy(e => e.OverallRank)
            .ThenBy(e => e.StudentName)
            .ToList();
    }

    public async Task<BigExamRankingEntry?> GetStudentRankingAsync(int examId, int studentId)
    {
        var all = await GetRankingsAsync(examId);

        return all.FirstOrDefault(e => e.StudentId == studentId);
    }

    public Task<List<Teacher>> ListTeachersForGraderAsync()
    {
        return _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();
    }

    public async Task<Teacher?> GetCurrentGraderAsync()
    {
        return await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.IsBigExamGrader);
    }

    /// <summary>
    /// Grants Big Exam grading access to exactly one teacher, revoking
    /// it from everyone else. Pass null to revoke access entirely.
    /// </summary>
    public async Task SetGraderAsync(int? teacherId)
    {
        var teachers = await _context.Teachers.ToListAsync();

        foreach (var teacher in teachers)
        {
            teacher.IsBigExamGrader = teacherId.HasValue && teacher.Id == teacherId.Value;
        }

        await _context.SaveChangesAsync();
    }
}
