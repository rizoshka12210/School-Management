using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

[Area("Parent")]
[Authorize(Roles = Roles.Parent)]
public class CalendarController : Controller
{
    private readonly AppDbContext _context;

    public CalendarController(AppDbContext context)
    {
        _context = context;
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
            CanManage = false,
            Events = events
        };

        return View(model);
    }
}
