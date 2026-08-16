using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class TopicsController : ParentControllerBase
{
    public TopicsController(
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
            return View(new List<Lesson>());
        }

        var lessons = await Context.Lessons
            .Where(l =>
                l.GroupId == student.GroupId &&
                l.Topic != null)
            .Include(l => l.Subject)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }
}
