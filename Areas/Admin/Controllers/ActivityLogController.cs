using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class ActivityLogController : Controller
{
    private readonly AppDbContext _context;

    public ActivityLogController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var entries = await _context.ActivityLogEntries
            .OrderByDescending(e => e.Timestamp)
            .Take(100)
            .ToListAsync();

        return View(entries);
    }
}
