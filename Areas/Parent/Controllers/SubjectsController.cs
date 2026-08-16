using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Parent;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class SubjectsController : ParentControllerBase
{
    public SubjectsController(
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

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        if (student.GroupId == null)
        {
            return View(new List<SubjectWithTeachers>());
        }

        var rows = await Context.Schedules
            .Where(s => s.GroupId == student.GroupId)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .ToListAsync();

        var subjects = rows
            .GroupBy(s => s.Subject.Name)
            .OrderBy(g => g.Key)
            .Select(g => new SubjectWithTeachers
            {
                SubjectName = g.Key,
                TeacherNames = g
                    .Select(x => x.Teacher.ApplicationUser.FullName)
                    .Distinct()
                    .ToList()
            })
            .ToList();

        return View(subjects);
    }
}
