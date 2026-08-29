using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class LeaderboardController : Controller
{
    private readonly LeaderboardService _leaderboard;

    public LeaderboardController(LeaderboardService leaderboard)
    {
        _leaderboard = leaderboard;
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

        ViewBag.Year = selectedYear;
        ViewBag.Month = selectedMonth;

        ViewBag.TopGrades = await _leaderboard.GetTopByGradeAsync(selectedYear, selectedMonth);
        ViewBag.TopAttendance = await _leaderboard.GetTopByAttendanceAsync(selectedYear, selectedMonth);
        ViewBag.MostImproved = await _leaderboard.GetMostImprovedAsync(selectedYear, selectedMonth);

        return View();
    }
}
