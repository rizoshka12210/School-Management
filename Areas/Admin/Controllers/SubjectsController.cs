using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SubjectsController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(s =>
                s.Name.ToLower().Contains(value));
        }

        ViewBag.Search = search;

        var subjects = await query
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(subjects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
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
        var exists = await _context.Subjects
            .AnyAsync(s =>
                s.Name.ToLower() ==
                model.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A subject with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();

            return View(model);
        }

        var subject = new Subject
        {
            Name = model.Name.Trim()
        };

        if (model.TeacherIds != null &&
            model.TeacherIds.Count > 0)
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

        TempData["Success"] =
            _localizer["Subject created successfully."].Value;

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
        SubjectFormViewModel model)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(
                s => s.Id == model.Id);

        if (subject == null)
        {
            return NotFound();
        }

        var exists = await _context.Subjects
            .AnyAsync(s =>
                s.Id != model.Id &&
                s.Name.ToLower() ==
                model.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A subject with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTeachersAsync();

            return View(model);
        }

        subject.Name = model.Name.Trim();

        subject.Teachers.Clear();

        if (model.TeacherIds != null &&
            model.TeacherIds.Count > 0)
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

        TempData["Success"] =
            _localizer["Subject updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Teachers)
                .ThenInclude(t => t.ApplicationUser)
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
    public async Task<IActionResult> DeleteConfirmed(
        int id)
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

        if (hasLessons ||
            hasSchedules ||
            hasGrades)
        {
            TempData["Error"] =
                _localizer["This subject cannot be deleted because it has lessons, schedules or grades."].Value;

            return RedirectToAction(nameof(Index));
        }

        _context.Subjects.Remove(subject);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Subject deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTeachersAsync()
    {
        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();
    }
}