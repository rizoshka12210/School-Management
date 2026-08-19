using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class CalendarController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public CalendarController(
        AppDbContext context,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
    {
        _context = context;
        _localizer = localizer;
        _activityLog = activityLog;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var now = DateTime.UtcNow;

        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        if (selectedMonth < 1 || selectedMonth > 12)
        {
            return BadRequest();
        }

        if (selectedYear < 2000 || selectedYear > 2100)
        {
            return BadRequest();
        }

        var start = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var events = await _context.CalendarEvents
            .Where(e => e.Date >= start && e.Date < end)
            .OrderBy(e => e.Date)
            .ToListAsync();

        var model = new CalendarGridViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            CanManage = true,
            Events = events
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create(DateTime? date)
    {
        var model = new CalendarEventFormViewModel
        {
            Date = DateOnly.FromDateTime(date ?? DateTime.UtcNow)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CalendarEventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var calendarEvent = new CalendarEvent
        {
            Title = model.Title.Trim(),
            Date = DateTime.SpecifyKind(
                model.Date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc),
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim()
        };

        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync($"Admin created event \"{calendarEvent.Title}\"");

        TempData["Success"] = _localizer["Event created successfully."].Value;

        return RedirectToAction(nameof(Index), new { year = calendarEvent.Date.Year, month = calendarEvent.Date.Month });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var calendarEvent = await _context.CalendarEvents.FindAsync(id);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        var model = new CalendarEventFormViewModel
        {
            Id = calendarEvent.Id,
            Title = calendarEvent.Title,
            Date = DateOnly.FromDateTime(calendarEvent.Date),
            Description = calendarEvent.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CalendarEventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var calendarEvent = await _context.CalendarEvents.FindAsync(model.Id);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        calendarEvent.Title = model.Title.Trim();
        calendarEvent.Date = DateTime.SpecifyKind(
            model.Date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        calendarEvent.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync($"Admin updated event \"{calendarEvent.Title}\"");

        TempData["Success"] = _localizer["Event updated successfully."].Value;

        return RedirectToAction(nameof(Index), new { year = calendarEvent.Date.Year, month = calendarEvent.Date.Month });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var calendarEvent = await _context.CalendarEvents.FindAsync(id);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        return View(calendarEvent);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var calendarEvent = await _context.CalendarEvents.FindAsync(id);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        var year = calendarEvent.Date.Year;
        var month = calendarEvent.Date.Month;
        var title = calendarEvent.Title;

        _context.CalendarEvents.Remove(calendarEvent);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync($"Admin deleted event \"{title}\"");

        TempData["Success"] = _localizer["Event deleted successfully."].Value;

        return RedirectToAction(nameof(Index), new { year, month });
    }
}
