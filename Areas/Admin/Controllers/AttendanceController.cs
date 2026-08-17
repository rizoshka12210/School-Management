using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Models.Enums;
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

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? date)
    {
        var attendances =
            await _attendanceService.GetAllAsync();

        var query = attendances.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(a =>
                $"{a.Student.FirstName} {a.Student.LastName}"
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Group.Name
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Subject.Name
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Teacher.ApplicationUser.FullName
                    .ToLower()
                    .Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AttendanceStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(a =>
                a.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(date) &&
            DateOnly.TryParse(
                date,
                out var parsedDate))
        {
            query = query.Where(a =>
                DateOnly.FromDateTime(
                    a.Lesson.StartTime) == parsedDate);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Date = date;

        return View(
            query
                .OrderByDescending(
                    a => a.Lesson.StartTime)
                .ToList());
    }
}