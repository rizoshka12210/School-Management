using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

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

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        ViewBag.AverageBySubject = grades
            .GroupBy(g => g.Subject.Name)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(x => x.Value), 2));

        ViewBag.OverallAverage = grades.Any()
            ? Math.Round(grades.Average(g => g.Value), 2)
            : 0;

        return View(grades);
    }
}
