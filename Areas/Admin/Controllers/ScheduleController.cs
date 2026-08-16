using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ScheduleEntity =
    SchoolManagementSystem.Web.Models.Entities.Schedule;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ScheduleController : Controller
{
    private readonly AppDbContext _context;

    public ScheduleController(AppDbContext context)
    {
        _context = context;
    }


    public async Task<IActionResult> Index()
    {
        var schedules = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Subject)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return View(schedules);
    }


    public async Task<IActionResult> Details(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }



    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();

        return View(new ScheduleFormViewModel
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        });
    }

    

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ScheduleFormViewModel model)
    {
        await ValidateScheduleAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();

            return View(model);
        }

        var schedule = new ScheduleEntity
        {
            DayOfWeek = model.DayOfWeek,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            GroupId = model.GroupId,
            TeacherId = model.TeacherId,
            SubjectId = model.SubjectId
        };

        _context.Schedules.Add(schedule);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

   

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        var model = new ScheduleFormViewModel
        {
            Id = schedule.Id,
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            GroupId = schedule.GroupId,
            TeacherId = schedule.TeacherId,
            SubjectId = schedule.SubjectId
        };

        await LoadLookupsAsync();

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ScheduleFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        await ValidateScheduleAsync(model, id);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();

            return View(model);
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        schedule.DayOfWeek = model.DayOfWeek;
        schedule.StartTime = model.StartTime;
        schedule.EndTime = model.EndTime;
        schedule.GroupId = model.GroupId;
        schedule.TeacherId = model.TeacherId;
        schedule.SubjectId = model.SubjectId;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

   

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }


    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        _context.Schedules.Remove(schedule);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }



    private async Task ValidateScheduleAsync(
        ScheduleFormViewModel model,
        int? currentId = null)
    {
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "End time must be later than start time.");
        }

        if (model.TeacherId <= 0 ||
            model.GroupId <= 0 ||
            model.SubjectId <= 0)
        {
            return;
        }

        // Teacher должен быть связан
        // и с Group, и с Subject
        var teacherValid = await _context.Teachers
            .AnyAsync(t =>
                t.Id == model.TeacherId &&
                t.Groups.Any(g => g.Id == model.GroupId) &&
                t.Subjects.Any(s => s.Id == model.SubjectId));

        if (!teacherValid)
        {
            ModelState.AddModelError(
                string.Empty,
                "The selected teacher must be assigned to both the selected group and subject.");
        }

        if (model.EndTime <= model.StartTime)
        {
            return;
        }

        // Проверяем конфликт Teacher
        var teacherConflict = await _context.Schedules
            .AnyAsync(s =>
                (!currentId.HasValue || s.Id != currentId.Value) &&
                s.DayOfWeek == model.DayOfWeek &&
                s.TeacherId == model.TeacherId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime);

        if (teacherConflict)
        {
            ModelState.AddModelError(
                string.Empty,
                "This teacher already has another lesson at this time.");
        }

        // Проверяем конфликт Group
        var groupConflict = await _context.Schedules
            .AnyAsync(s =>
                (!currentId.HasValue || s.Id != currentId.Value) &&
                s.DayOfWeek == model.DayOfWeek &&
                s.GroupId == model.GroupId &&
                s.StartTime < model.EndTime &&
                model.StartTime < s.EndTime);

        if (groupConflict)
        {
            ModelState.AddModelError(
                string.Empty,
                "This group already has another lesson at this time.");
        }
    }


    private async Task LoadLookupsAsync()
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();

        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.ApplicationUser.FullName
            })
            .ToListAsync();

        ViewBag.Subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToListAsync();
    }
}