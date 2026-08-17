using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class AttendanceController : Controller
{
    private readonly AttendanceService _attendanceService;

    public AttendanceController(
        AttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    public async Task<IActionResult> Index()
    {
        var attendances =
            await _attendanceService.GetAllAsync();

        return View(attendances);
    }
}