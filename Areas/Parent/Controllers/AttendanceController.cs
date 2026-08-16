using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class AttendanceController : ParentControllerBase
{
    public AttendanceController(
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

        var attendances = await Context.Attendances
            .Where(a => a.StudentId == resolvedId)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .OrderByDescending(a => a.Lesson.StartTime)
            .ToListAsync();

        var total = attendances.Count;

        var present = attendances
            .Count(a => a.Status == AttendanceStatus.Present);

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
        ViewBag.TotalLessons = total;
        ViewBag.PresentCount = present;

        ViewBag.AttendanceRate = total == 0
            ? 0
            : Math.Round(present * 100.0 / total, 1);

        return View(attendances);
    }
}
