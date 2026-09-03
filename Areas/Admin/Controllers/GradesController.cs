using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class GradesController : Controller
{
    private readonly GradeService _gradeService;
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public GradesController(
        GradeService gradeService,
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
    {
        _gradeService = gradeService;
        _context = context;
        _localizer = localizer;
        _activityLog = activityLog;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? subjectId)
    {
        var grades =
            await _gradeService.GetAllAsync();

        ViewBag.Subjects = grades
            .Select(g => g.Subject)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.Name)
            .ToList();

        var query = grades.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(g =>
                $"{g.Student.FirstName} {g.Student.LastName}"
                    .ToLower()
                    .Contains(value) ||
                g.Subject.Name
                    .ToLower()
                    .Contains(value) ||
                g.Teacher.ApplicationUser.FullName
                    .ToLower()
                    .Contains(value));
        }

        if (subjectId.HasValue)
        {
            query = query.Where(g =>
                g.SubjectId == subjectId.Value);
        }

        ViewBag.Search = search;
        ViewBag.SubjectId = subjectId;

        var examGradeHistory = await _context.ExamGrades
            .Include(e => e.Student)
            .Include(e => e.Subject)
            .Include(e => e.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .ToListAsync();

        ViewBag.CurrentExamGradeIds = GradeAveragingHelper
            .LatestPerStudentSubject(examGradeHistory)
            .Select(e => e.Id)
            .ToHashSet();

        var examQuery = examGradeHistory.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            examQuery = examQuery.Where(e =>
                $"{e.Student.FirstName} {e.Student.LastName}"
                    .ToLower()
                    .Contains(value) ||
                e.Subject.Name
                    .ToLower()
                    .Contains(value) ||
                e.Teacher.ApplicationUser.FullName
                    .ToLower()
                    .Contains(value));
        }

        if (subjectId.HasValue)
        {
            examQuery = examQuery.Where(e => e.SubjectId == subjectId.Value);
        }

        ViewBag.ExamGradeHistory = examQuery
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.Id)
            .ToList();

        return View(
            query
                .OrderByDescending(g => g.Date)
                .ToList());
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var grade = await _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound();
        }

        var model = new GradeFormViewModel
        {
            Id = grade.Id,
            StudentName = $"{grade.Student.FirstName} {grade.Student.LastName}",
            SubjectName = grade.Subject.Name,
            TeacherName = grade.Teacher.ApplicationUser.FullName,
            Date = grade.Date,
            Value = grade.Value,
            Comment = grade.Comment
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(GradeFormViewModel model)
    {
        var grade = await _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == model.Id);

        if (grade == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.StudentName =
                $"{grade.Student.FirstName} {grade.Student.LastName}";
            model.SubjectName = grade.Subject.Name;
            model.TeacherName = grade.Teacher.ApplicationUser.FullName;
            model.Date = grade.Date;

            return View(model);
        }

        grade.Value = model.Value;
        grade.Comment = string.IsNullOrWhiteSpace(model.Comment)
            ? null
            : model.Comment.Trim();

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            $"Admin updated grade {grade.Value} for {grade.Student.FirstName} {grade.Student.LastName}");

        TempData["Success"] = _localizer["Grade updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var grade = await _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound();
        }

        return View(grade);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound();
        }

        _context.Grades.Remove(grade);

        await _context.SaveChangesAsync();

        TempData["Success"] = _localizer["Grade deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }
}