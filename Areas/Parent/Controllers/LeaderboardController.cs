using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class LeaderboardController : ParentControllerBase
{
    private readonly LeaderboardService _leaderboard;

    public LeaderboardController(
        AppDbContext context,
        OwnershipHelper ownership,
        LeaderboardService leaderboard)
        : base(context, ownership)
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
