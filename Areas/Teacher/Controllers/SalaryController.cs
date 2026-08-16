using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class SalaryController : TeacherControllerBase
{
    private readonly SalaryService _salaryService;

    public SalaryController(
        AppDbContext context,
        OwnershipHelper ownership,
        SalaryService salaryService)
        : base(context, ownership)
    {
        _salaryService = salaryService;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        if (selectedMonth < 1 || selectedMonth > 12)
        {
            return BadRequest();
        }

        var summary = await _salaryService.CalculateAsync(
            teacherId.Value,
            selectedYear,
            selectedMonth);

        if (summary == null)
        {
            return NotFound();
        }

        return View(summary);
    }
}
