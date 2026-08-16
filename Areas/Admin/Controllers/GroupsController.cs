using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class GroupsController : Controller
{
    private readonly AppDbContext _context;

    public GroupsController(AppDbContext context)
    {
        _context = context;
    }


    public async Task<IActionResult> Index()
    {
        var groups = await _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }


    public async Task<IActionResult> Details(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadTeachersAsync();

        return View(new GroupFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();
            return View(model);
        }

        var nameExists = await _context.Groups
            .AnyAsync(g => g.Name == model.Name);

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A group with this name already exists.");

            await LoadTeachersAsync();

            return View(model);
        }

        var group = new Group
        {
            Name = model.Name.Trim()
        };

        if (model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t => model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                group.Teachers.Add(teacher);
            }
        }

        _context.Groups.Add(group);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var model = new GroupFormViewModel
        {
            Id = group.Id,
            Name = group.Name,
            TeacherIds = group.Teachers
                .Select(t => t.Id)
                .ToList()
        };

        await LoadTeachersAsync();

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        GroupFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();
            return View(model);
        }

        var group = await _context.Groups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var nameExists = await _context.Groups
            .AnyAsync(g =>
                g.Name == model.Name &&
                g.Id != id);

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A group with this name already exists.");

            await LoadTeachersAsync();

            return View(model);
        }

        group.Name = model.Name.Trim();

        group.Teachers.Clear();

        if (model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t => model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                group.Teachers.Add(teacher);
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

  
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Students)
            .Include(g => g.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        var hasLessons = await _context.Lessons
            .AnyAsync(l => l.GroupId == id);

        var hasSchedules = await _context.Schedules
            .AnyAsync(s => s.GroupId == id);

        if (hasLessons || hasSchedules)
        {
            TempData["Error"] =
                "This group cannot be deleted because it is used in lessons or schedule.";

            return RedirectToAction(nameof(Index));
        }

        _context.Groups.Remove(group);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    private async Task LoadTeachersAsync()
    {
        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.ApplicationUser.FullName
            })
            .ToListAsync();
    }
}