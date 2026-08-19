using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class StudentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly StudentRiskService _riskService;

    public StudentsController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer,
        StudentRiskService riskService)
    {
        _context = context;
        _localizer = localizer;
        _riskService = riskService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Students
            .Include(s => s.Group)
            .Include(s => s.Parents)
                .ThenInclude(p => p.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(s =>
                s.FirstName.ToLower().Contains(value) ||
                s.LastName.ToLower().Contains(value) ||
                (s.Group != null && s.Group.Name.ToLower().Contains(value)));
        }

        ViewBag.Search = search;

        var students = await query
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();

        return View(students);
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await _context.Students
            .Include(s => s.Group)
            .Include(s => s.Parents)
                .ThenInclude(p => p.ApplicationUser)
            .Include(s => s.Grades)
                .ThenInclude(g => g.Subject)
            .Include(s => s.Grades)
                .ThenInclude(g => g.Teacher)
                    .ThenInclude(t => t.ApplicationUser)
            .Include(s => s.Attendances)
                .ThenInclude(a => a.Lesson)
                    .ThenInclude(l => l.Subject)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        var risk = _riskService.Evaluate(student);

        var model = new StudentProfile360ViewModel
        {
            Student = student,
            AverageGrade = risk.AverageGrade,
            AttendanceRate = risk.AttendanceRate,
            Risk = risk,
            RecentGrades = student.Grades
                .OrderByDescending(g => g.Date)
                .Take(8)
                .ToList(),
            RecentAttendance = student.Attendances
                .OrderByDescending(a => a.Lesson.StartTime)
                .Take(14)
                .ToList(),
            TeacherComments = student.Grades
                .Where(g => !string.IsNullOrWhiteSpace(g.Comment))
                .OrderByDescending(g => g.Date)
                .Take(6)
                .Select(g => new TeacherCommentViewModel
                {
                    TeacherName = g.Teacher.ApplicationUser.FullName,
                    SubjectName = g.Subject.Name,
                    Comment = g.Comment!,
                    Date = g.Date
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();

        var model = new StudentFormViewModel
        {
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return View(model);
        }

        var student = new Student
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            DateOfBirth = DateTime.SpecifyKind(
                model.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc),
            GroupId = model.GroupId
        };

        if (model.ParentIds != null && model.ParentIds.Count > 0)
        {
            var parents = await _context.Parents
                .Where(p => model.ParentIds.Contains(p.Id))
                .ToListAsync();

            foreach (var parent in parents)
            {
                student.Parents.Add(parent);
            }
        }

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Student created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        var model = new StudentFormViewModel
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            DateOfBirth = DateOnly.FromDateTime(student.DateOfBirth),
            GroupId = student.GroupId,
            ParentIds = student.Parents.Select(p => p.Id).ToList()
        };

        await LoadFormDataAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return View(model);
        }

        var student = await _context.Students
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (student == null)
        {
            return NotFound();
        }

        student.FirstName = model.FirstName.Trim();
        student.LastName = model.LastName.Trim();
        student.DateOfBirth = DateTime.SpecifyKind(
            model.DateOfBirth.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        student.GroupId = model.GroupId;

        student.Parents.Clear();

        if (model.ParentIds != null && model.ParentIds.Count > 0)
        {
            var parents = await _context.Parents
                .Where(p => model.ParentIds.Contains(p.Id))
                .ToListAsync();

            foreach (var parent in parents)
            {
                student.Parents.Add(parent);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Student updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students
            .Include(s => s.Group)
            .Include(s => s.Parents)
                .ThenInclude(p => p.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Student deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormDataAsync()
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.Parents = await _context.Parents
            .Include(p => p.ApplicationUser)
            .OrderBy(p => p.ApplicationUser.FullName)
            .ToListAsync();
    }
}
