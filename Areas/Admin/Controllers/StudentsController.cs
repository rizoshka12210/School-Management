using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class StudentsController : Controller
{
    private readonly AppDbContext _context;

    public StudentsController(AppDbContext context)
    {
        _context = context;
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
                (
                    s.Group != null &&
                    s.Group.Name.ToLower().Contains(value)
                ));
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
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();

        var model = new StudentFormViewModel
        {
            DateOfBirth = DateOnly.FromDateTime(
                DateTime.UtcNow.AddYears(-10))
        };

        return View(model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        StudentFormViewModel model)
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
                model.DateOfBirth.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Utc),

            GroupId = model.GroupId
        };


  

        if (model.ParentIds != null &&
            model.ParentIds.Count > 0)
        {
            var parents = await _context.Parents
                .Where(p =>
                    model.ParentIds.Contains(p.Id))
                .ToListAsync();

            foreach (var parent in parents)
            {
                student.Parents.Add(parent);
            }
        }


        _context.Students.Add(student);

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Student created successfully.";

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

            DateOfBirth =
                DateOnly.FromDateTime(
                    student.DateOfBirth),

            GroupId = student.GroupId,

            ParentIds = student.Parents
                .Select(p => p.Id)
                .ToList()
        };


        await LoadFormDataAsync();

        return View(model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();

            return View(model);
        }


        var student = await _context.Students
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(
                s => s.Id == model.Id);

        if (student == null)
        {
            return NotFound();
        }


        student.FirstName =
            model.FirstName.Trim();

        student.LastName =
            model.LastName.Trim();

        student.DateOfBirth =
            DateTime.SpecifyKind(
                model.DateOfBirth.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Utc);

        student.GroupId =
            model.GroupId;


     

        student.Parents.Clear();


  

        if (model.ParentIds != null &&
            model.ParentIds.Count > 0)
        {
            var parents = await _context.Parents
                .Where(p =>
                    model.ParentIds.Contains(p.Id))
                .ToListAsync();

            foreach (var parent in parents)
            {
                student.Parents.Add(parent);
            }
        }


        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Student updated successfully.";

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students
            .Include(s => s.Group)
            .Include(s => s.Parents)
                .ThenInclude(p => p.ApplicationUser)
            .FirstOrDefaultAsync(
                s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }


        _context.Students.Remove(student);

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Student deleted successfully.";

        return RedirectToAction(nameof(Index));
    }



    private async Task LoadFormDataAsync()
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();


        ViewBag.Parents = await _context.Parents
            .Include(p => p.ApplicationUser)
            .OrderBy(p =>
                p.ApplicationUser.FullName)
            .ToListAsync();
    }
}