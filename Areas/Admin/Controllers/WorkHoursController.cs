using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class WorkHoursController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkHoursController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        int? year,
        int? month,
        int? teacherId)
    {
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        if (selectedMonth < 1 || selectedMonth > 12 ||
            selectedYear < 2000 || selectedYear > 2100)
        {
            return BadRequest();
        }

        var monthStart = new DateTime(
            selectedYear,
            selectedMonth,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var query = _context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Where(l => l.StartTime >= monthStart && l.StartTime < monthEnd)
            .AsQueryable();

        if (teacherId.HasValue)
        {
            query = query.Where(l => l.TeacherId == teacherId.Value);
        }

        ViewBag.Year = selectedYear;
        ViewBag.Month = selectedMonth;
        ViewBag.TeacherId = teacherId;
        ViewBag.Teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();

        var lessons = await query
            .OrderByDescending(l => l.StartTime)
            .ThenBy(l => l.Teacher.ApplicationUser.FullName)
            .ToListAsync();

        return View(lessons);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int lessonId,
        TimeOnly startTime,
        TimeOnly endTime,
        int? year,
        int? month,
        int? teacherId)
    {
        if (endTime <= startTime)
        {
            TempData["Error"] =
                _localizer["End time must be later than start time."].Value;

            return RedirectToAction(nameof(Index), new { year, month, teacherId });
        }

        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var lessonDate = DateOnly.FromDateTime(lesson.StartTime);
        var newStart = DateTime.SpecifyKind(
            lessonDate.ToDateTime(startTime),
            DateTimeKind.Utc);
        var newEnd = DateTime.SpecifyKind(
            lessonDate.ToDateTime(endTime),
            DateTimeKind.Utc);

        var conflict = await _context.Lessons
            .Where(l =>
                l.Id != lesson.Id &&
                l.TeacherId == lesson.TeacherId &&
                l.StartTime < newEnd &&
                newStart < l.EndTime)
            .Include(l => l.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(l => l.Subject)
            .Include(l => l.Group)
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            TempData["Error"] = _localizer[
                "Teacher {0} already has {1} with {2} at {3}–{4}.",
                conflict.Teacher.ApplicationUser.FullName,
                conflict.Subject.Name,
                conflict.Group.Name,
                conflict.StartTime.ToString("HH:mm"),
                conflict.EndTime.ToString("HH:mm")].Value;

            return RedirectToAction(nameof(Index), new { year, month, teacherId });
        }

        lesson.StartTime = newStart;
        lesson.EndTime = newEnd;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Lesson updated successfully."].Value;

        return RedirectToAction(nameof(Index), new { year, month, teacherId });
    }
}
