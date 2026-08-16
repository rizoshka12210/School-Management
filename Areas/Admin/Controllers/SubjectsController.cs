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
public class SubjectsController : Controller
{
    private readonly AppDbContext _context;

    public SubjectsController(AppDbContext context)
    {
        _context = context;
    }

  
    public async Task<IActionResult> Index()
    {
        var subjects = await _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(subjects);
    }


    public async Task<IActionResult> Details(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Lessons)
            .Include(s => s.Schedules)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadTeachersAsync();

        return View(new SubjectFormViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SubjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();

            return View(model);
        }

        var name = model.Name.Trim();

        var normalizedName = name.ToLower();

        var subjectExists = await _context.Subjects
            .AnyAsync(s =>
                s.Name.ToLower() == normalizedName);

        if (subjectExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A subject with this name already exists.");

            await LoadTeachersAsync();

            return View(model);
        }

        var subject = new Subject
        {
            Name = name
        };

        if (model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t =>
                    model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                subject.Teachers.Add(teacher);
            }
        }

        _context.Subjects.Add(subject);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        var model = new SubjectFormViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            TeacherIds = subject.Teachers
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
        SubjectFormViewModel model)
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

        var subject = await _context.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        var name = model.Name.Trim();
        var normalizedName = name.ToLower();

        var subjectExists = await _context.Subjects
            .AnyAsync(s =>
                s.Id != id &&
                s.Name.ToLower() == normalizedName);

        if (subjectExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A subject with this name already exists.");

            await LoadTeachersAsync();

            return View(model);
        }

        subject.Name = name;

        subject.Teachers.Clear();

        if (model.TeacherIds.Count > 0)
        {
            var teachers = await _context.Teachers
                .Where(t =>
                    model.TeacherIds.Contains(t.Id))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                subject.Teachers.Add(teacher);
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Lessons)
            .Include(s => s.Schedules)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }


    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        var hasLessons = await _context.Lessons
            .AnyAsync(l => l.SubjectId == id);

        var hasSchedules = await _context.Schedules
            .AnyAsync(s => s.SubjectId == id);

        var hasGrades = await _context.Grades
            .AnyAsync(g => g.SubjectId == id);

        if (hasLessons || hasSchedules || hasGrades)
        {
            TempData["Error"] =
                "This subject cannot be deleted because it is already used in lessons, schedules or grades.";

            return RedirectToAction(nameof(Index));
        }

        _context.Subjects.Remove(subject);

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