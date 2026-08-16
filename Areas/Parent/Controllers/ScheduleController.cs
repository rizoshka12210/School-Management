using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class ScheduleController : ParentControllerBase
{
    public ScheduleController(
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

        ViewBag.StudentId = student.Id;
        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        if (student.GroupId == null)
        {
            return View(new List<Schedule>());
        }

        var schedule = await Context.Schedules
            .Where(s => s.GroupId == student.GroupId)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return View(schedule);
    }
}
