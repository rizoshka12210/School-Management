using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class GroupsController : TeacherControllerBase
{
    public GroupsController(
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

        var groups = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }

    public async Task<IActionResult> Details(int id)
    {
        var owns = await Ownership.TeacherOwnsGroupAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var group = await Context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var subjects = await Context.Schedules
            .Where(s => s.GroupId == id)
            .Select(s => s.Subject.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        ViewBag.Subjects = subjects;

        return View(group);
    }
}
