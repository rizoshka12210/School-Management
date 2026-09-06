using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class GradesController : ParentControllerBase
{
    private readonly ExamSheetService _examSheetService;

    public GradesController(
        AppDbContext context,
        OwnershipHelper ownership,
        ExamSheetService examSheetService)
        : base(context, ownership)
    {
        _examSheetService = examSheetService;
    }

    public async Task<IActionResult> Index(int? studentId)
    {
        var resolvedId = await ResolveStudentIdAsync(studentId);

        if (resolvedId == null)
        {
            return Forbid();
        }

        var student = await Context.Students
            .FirstOrDefaultAsync(s => s.Id == resolvedId);

        if (student == null)
        {
            return NotFound();
        }

        var grades = await Context.Grades
            .Where(g => g.StudentId == resolvedId)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .OrderByDescending(g => g.Date)
            .ToListAsync();

        var examGradeHistory = await Context.ExamGrades
            .Where(e => e.StudentId == resolvedId)
            .Include(e => e.Subject)
            .Include(e => e.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        var examGrades = GradeAveragingHelper.LatestPerStudentSubject(examGradeHistory);

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
        ViewBag.ExamGradeHistory = examGradeHistory;
        ViewBag.CurrentExamGradeIds = examGrades.Select(e => e.Id).ToHashSet();

        var relevantGroupIds = examGradeHistory.Select(e => e.GroupId).Distinct().ToList();

        var thresholds = await Context.ExamBlacklistThresholds
            .AsNoTracking()
            .Where(t => relevantGroupIds.Contains(t.GroupId))
            .ToListAsync();

        ViewBag.ExamBlacklistThresholds = thresholds
            .ToDictionary(t => (t.GroupId, t.SubjectId), t => t.Threshold);

        var subjectNames = grades.Select(g => g.Subject.Name)
            .Union(examGrades.Select(e => e.Subject.Name))
            .Distinct()
            .OrderBy(name => name);

        ViewBag.AverageBySubject = subjectNames
            .ToDictionary(
                name => name,
                name => GradeAveragingHelper.Combine(
                    grades.Where(g => g.Subject.Name == name).Select(g => g.Value),
                    examGrades.Where(e => e.Subject.Name == name).Select(e => e.Average)) ?? 0);

        ViewBag.OverallAverage = GradeAveragingHelper.Combine(
            grades.Select(g => g.Value),
            examGrades.Select(e => e.Average)) ?? 0;

        return View(grades);
    }

    [HttpGet]
    public async Task<IActionResult> Rankings(int? subjectId)
    {
        var subjects = await Context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (!subjects.Any())
        {
            return NotFound();
        }

        var resolvedSubjectId = subjectId.HasValue && subjects.Any(s => s.Id == subjectId.Value)
            ? subjectId.Value
            : subjects.First().Id;

        ViewBag.Subjects = subjects;
        ViewBag.SubjectId = resolvedSubjectId;
        ViewBag.MyStudentIds = await GetOwnedStudentIdsAsync();

        var rankings = await _examSheetService.GetRankingsAsync(resolvedSubjectId);

        var groupIds = rankings.Select(r => r.GroupId).Distinct().ToList();

        var thresholds = await Context.ExamBlacklistThresholds
            .Where(t => t.SubjectId == resolvedSubjectId && groupIds.Contains(t.GroupId))
            .ToListAsync();

        ViewBag.ExamBlacklistThresholds = thresholds
            .ToDictionary(t => (t.GroupId, t.SubjectId), t => t.Threshold);

        return View(rankings);
    }
}
