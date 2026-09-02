using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class GradesController : ParentControllerBase
{
    public GradesController(
        AppDbContext context,
        OwnershipHelper ownership)
        : base(context, ownership)
    {
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

        var examGrades = await Context.ExamGrades
            .Where(e => e.StudentId == resolvedId)
            .Include(e => e.Subject)
            .ToListAsync();

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

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
}
