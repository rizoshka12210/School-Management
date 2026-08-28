using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class WorkHoursController : TeacherControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkHoursController(
        AppDbContext context,
        OwnershipHelper ownership,
        IStringLocalizer<SharedResource> localizer)
        : base(context, ownership)
    {
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        int? year,
        int? month)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

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

        ViewBag.Year = selectedYear;
        ViewBag.Month = selectedMonth;

        var lessons = await Context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Where(l =>
                l.TeacherId == teacherId.Value &&
                l.StartTime >= monthStart &&
                l.StartTime < monthEnd)
            .OrderByDescending(l => l.StartTime)
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
        int? month)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        if (endTime <= startTime)
        {
            TempData["Error"] =
                _localizer["End time must be later than start time."].Value;

            return RedirectToAction(nameof(Index), new { year, month });
        }

        var lesson = await Context.Lessons
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.TeacherId == teacherId.Value);

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

        var conflict = await Context.Lessons
            .Where(l =>
                l.Id != lesson.Id &&
                l.TeacherId == teacherId.Value &&
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

            return RedirectToAction(nameof(Index), new { year, month });
        }

        lesson.StartTime = newStart;
        lesson.EndTime = newEnd;

        await Context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Lesson updated successfully."].Value;

        return RedirectToAction(nameof(Index), new { year, month });
    }
}
