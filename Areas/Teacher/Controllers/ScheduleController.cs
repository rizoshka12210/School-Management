using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class ScheduleController : TeacherControllerBase
{
    public ScheduleController(
        AppDbContext context,
        OwnershipHelper ownership)
        : base(context, ownership)
    {
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var schedule = await Context.Schedules
            .Where(s => s.TeacherId == teacherId)
            .Include(s => s.Group)
            .Include(s => s.Subject)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var headTeacherSubjectIds = await Ownership.GetCurrentUserHeadTeacherSubjectIdsAsync(User);

        ViewBag.HeadTeacherSubjects = headTeacherSubjectIds.Any()
            ? await Context.Subjects
                .Where(s => headTeacherSubjectIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .ToListAsync()
            : new List<SchoolManagementSystem.Web.Models.Entities.Subject>();

        return View(schedule);
    }
}
