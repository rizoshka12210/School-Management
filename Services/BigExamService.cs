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

    /// <summary>Null means this entry is the total across every subject.</summary>
    public int? SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

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

public class BigExamSubjectStatus
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public int StudentsGraded { get; set; }
}

public class BigExamGroupOverview
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int StudentsCount { get; set; }

    public List<BigExamSubjectStatus> Subjects { get; set; } = new();
}

/// <summary>
/// Manages the periodic school-wide Big Exam: it covers every subject
/// taught at the center (4 subjects means 4 scores per student, not
/// one blended number), organized the same way as the regular exam
/// sheets - Group -> Subject -> students. Only Admin and the single
/// designated teacher (Teacher.IsBigExamGrader) are ever allowed to
/// call the write methods here - that check happens in the calling
/// controllers. Everyone (Admin, Director, the designated teacher and
/// parents) can read rankings and history.
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

    /// <summary>Collapses append-only history down to the latest row per student+subject.</summary>
    public static List<BigExamGrade> LatestPerStudentSubject(IEnumerable<BigExamGrade> grades)
    {
        return grades
            .GroupBy(g => new { g.StudentId, g.SubjectId })
            .Select(g => g
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .First())
            .ToList();
    }

    /// <summary>Every subject taught at the center - the Big Exam always covers all of them.</summary>
    public Task<List<Subject>> ListSubjectsAsync()
    {
        return _context.Subjects.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<List<BigExamGroupOverview>> GetGroupOverviewAsync(int examId)
    {
        var groups = await _context.Groups
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync();

        var subjects = await ListSubjectsAsync();

        var history = LatestPerStudentSubject(
            await _context.BigExamGrades
                .Where(g => g.BigExamId == examId)
                .ToListAsync());

        return groups.Select(group => new BigExamGroupOverview
        {
            GroupId = group.Id,
            GroupName = group.Name,
            StudentsCount = group.Students.Count,
            Subjects = subjects.Select(subject => new BigExamSubjectStatus
            {
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                StudentsGraded = history.Count(g =>
                    g.GroupId == group.Id &&
                    g.SubjectId == subject.Id &&
                    g.Score.HasValue)
            }).ToList()
        }).ToList();
    }

    public async Task<BigExamSheetViewModel?> BuildSheetAsync(int examId, int groupId, int subjectId)
    {
        var exam = await _context.BigExams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);

        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var subject = await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subjectId);

        if (exam == null || group == null || subject == null)
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
            .Where(g => g.BigExamId == examId && g.GroupId == groupId && g.SubjectId == subjectId)
            .Include(g => g.Teacher)
                .ThenInclude(t => t!.ApplicationUser)
            .ToListAsync();

        var latestByStudent = LatestPerStudentSubject(history).ToDictionary(g => g.StudentId);
        var currentIds = latestByStudent.Values.Select(g => g.Id).ToHashSet();

        var studentNames = students.ToDictionary(
            s => s.Id,
            s => $"{s.FirstName} {s.LastName}");

        var model = new BigExamSheetViewModel
        {
            BigExamId = exam.Id,
            BigExamTitle = exam.Title,
            GroupId = group.Id,
            GroupName = group.Name,
            SubjectId = subject.Id,
            SubjectName = subject.Name
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
        int subjectId,
        int? teacherId,
        IEnumerable<BigExamSheetRowViewModel> rows)
    {
        var examExists = await _context.BigExams.AnyAsync(e => e.Id == examId);
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == subjectId);

        if (!examExists || !groupExists || !subjectExists)
        {
            return false;
        }

        var validStudentIds = (await _context.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();

        var existing = await _context.BigExamGrades
            .Where(g => g.BigExamId == examId && g.GroupId == groupId && g.SubjectId == subjectId)
            .ToListAsync();

        var latestByStudent = LatestPerStudentSubject(existing).ToDictionary(g => g.StudentId);

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
                SubjectId = subjectId,
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
    /// Pass a subjectId for that subject's ranking, or null for the
    /// "Overall Total" ranking - the sum of each student's latest score
    /// across every subject they have been graded in for this exam.
    /// </summary>
    public async Task<List<BigExamRankingEntry>> GetRankingsAsync(int examId, int? subjectId)
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
            .Include(g => g.Subject)
            .ToListAsync();

        var latest = LatestPerStudentSubject(history)
            .Where(g => g.Score.HasValue)
            .ToList();

        List<(int StudentId, int GroupId, string GroupName, string StudentName, decimal Score)> scored;
        string subjectName;

        if (subjectId.HasValue)
        {
            subjectName = latest.FirstOrDefault(g => g.SubjectId == subjectId.Value)?.Subject.Name
                ?? await _context.Subjects
                    .Where(s => s.Id == subjectId.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync()
                ?? string.Empty;

            scored = latest
                .Where(g => g.SubjectId == subjectId.Value)
                .Select(g => (
                    g.StudentId,
                    g.GroupId,
                    g.Group.Name,
                    $"{g.Student.FirstName} {g.Student.LastName}",
                    g.Score!.Value))
                .ToList();
        }
        else
        {
            subjectName = "Overall Total";

            scored = latest
                .GroupBy(g => g.StudentId)
                .Select(group =>
                {
                    var first = group.First();

                    return (
                        StudentId: group.Key,
                        GroupId: first.GroupId,
                        GroupName: first.Group.Name,
                        StudentName: $"{first.Student.FirstName} {first.Student.LastName}",
                        Score: group.Sum(g => g.Score!.Value));
                })
                .ToList();
        }

        var overallSize = scored.Count;
        var entries = new List<BigExamRankingEntry>();

        foreach (var group in scored.GroupBy(s => s.GroupId))
        {
            var groupSize = group.Count();

            foreach (var s in group)
            {
                var overallRank = 1 + scored.Count(o => o.Score > s.Score);
                var groupRank = 1 + group.Count(o => o.Score > s.Score);

                entries.Add(new BigExamRankingEntry
                {
                    BigExamId = exam.Id,
                    BigExamTitle = exam.Title,
                    BigExamDate = exam.Date,
                    SubjectId = subjectId,
                    SubjectName = subjectName,
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    GroupId = s.GroupId,
                    GroupName = s.GroupName,
                    Score = s.Score,
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

    /// <summary>
    /// Every ranking entry (one per subject, plus the overall total) for
    /// one student in one Big Exam - used by the parent's read-only view.
    /// </summary>
    public async Task<List<BigExamRankingEntry>> GetStudentRankingsAsync(int examId, int studentId)
    {
        var subjects = await ListSubjectsAsync();
        var results = new List<BigExamRankingEntry>();

        foreach (var subject in subjects)
        {
            var all = await GetRankingsAsync(examId, subject.Id);
            var mine = all.FirstOrDefault(e => e.StudentId == studentId);

            if (mine != null)
            {
                results.Add(mine);
            }
        }

        var total = await GetRankingsAsync(examId, null);
        var myTotal = total.FirstOrDefault(e => e.StudentId == studentId);

        if (myTotal != null)
        {
            results.Add(myTotal);
        }

        return results;
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
