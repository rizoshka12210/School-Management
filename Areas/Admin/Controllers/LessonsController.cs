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
public class LessonsController : Controller
{
    private readonly AppDbContext _context;

    public LessonsController(AppDbContext context)
    {
        _context = context;
    }



    public async Task<IActionResult> Index()
    {
        var lessons = await _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(l => l.Subject)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }



    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        return View(lesson);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();

        return View(new LessonFormViewModel
        {
            StartTime = DateTime.Today.AddHours(9),
            EndTime = DateTime.Today.AddHours(10)
        });
    }

 

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LessonFormViewModel model)
    {
        ValidateTimes(model);

        await ValidateTeacherRelationshipAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();

            return View(model);
        }

        var lesson = new Lesson
        {
            StartTime = MakeUtc(model.StartTime),

            EndTime = MakeUtc(model.EndTime),

            Topic = string.IsNullOrWhiteSpace(model.Topic)
                ? null
                : model.Topic.Trim(),

            GroupId = model.GroupId,
            TeacherId = model.TeacherId,
            SubjectId = model.SubjectId
        };

        _context.Lessons.Add(lesson);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

   

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        var model = new LessonFormViewModel
        {
            Id = lesson.Id,
            StartTime = lesson.StartTime,
            EndTime = lesson.EndTime,
            Topic = lesson.Topic,
            GroupId = lesson.GroupId,
            TeacherId = lesson.TeacherId,
            SubjectId = lesson.SubjectId
        };

        await LoadLookupsAsync();

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        LessonFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ValidateTimes(model);

        await ValidateTeacherRelationshipAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();

            return View(model);
        }

        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        lesson.StartTime =
            MakeUtc(model.StartTime);

        lesson.EndTime =
            MakeUtc(model.EndTime);

        lesson.Topic =
            string.IsNullOrWhiteSpace(model.Topic)
                ? null
                : model.Topic.Trim();

        lesson.GroupId = model.GroupId;
        lesson.TeacherId = model.TeacherId;
        lesson.SubjectId = model.SubjectId;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        return View(lesson);
    }



    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        // Не удаляем урок, если Teacher уже
        // поставил посещаемость или оценки.
        if (lesson.Attendances.Any() ||
            lesson.Grades.Any())
        {
            TempData["Error"] =
                "This lesson cannot be deleted because it already has attendance or grades.";

            return RedirectToAction(nameof(Index));
        }

        _context.Lessons.Remove(lesson);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    private void ValidateTimes(
        LessonFormViewModel model)
    {
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "End time must be later than start time.");
        }
    }

    private async Task ValidateTeacherRelationshipAsync(
        LessonFormViewModel model)
    {
        if (model.TeacherId <= 0 ||
            model.GroupId <= 0 ||
            model.SubjectId <= 0)
        {
            return;
        }

        var teacherHasAccess =
            await _context.Teachers
                .AnyAsync(t =>
                    t.Id == model.TeacherId &&
                    t.Groups.Any(
                        g => g.Id == model.GroupId) &&
                    t.Subjects.Any(
                        s => s.Id == model.SubjectId));

        if (!teacherHasAccess)
        {
            ModelState.AddModelError(
                string.Empty,
                "The selected teacher must be assigned to both the selected group and subject.");
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


    private static DateTime MakeUtc(
        DateTime dateTime)
    {
        return DateTime.SpecifyKind(
            dateTime,
            DateTimeKind.Utc);
    }
}