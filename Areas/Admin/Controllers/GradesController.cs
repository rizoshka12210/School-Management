using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class GradesController : Controller
{
    private readonly GradeService _gradeService;

    public GradesController(
        GradeService gradeService)
    {
        _gradeService = gradeService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? subjectId)
    {
        var grades =
            await _gradeService.GetAllAsync();

        ViewBag.Subjects = grades
            .Select(g => g.Subject)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.Name)
            .ToList();

        var query = grades.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(g =>
                $"{g.Student.FirstName} {g.Student.LastName}"
                    .ToLower()
                    .Contains(value) ||
                g.Subject.Name
                    .ToLower()
                    .Contains(value) ||
                g.Teacher.ApplicationUser.FullName
                    .ToLower()
                    .Contains(value));
        }

        if (subjectId.HasValue)
        {
            query = query.Where(g =>
                g.SubjectId == subjectId.Value);
        }

        ViewBag.Search = search;
        ViewBag.SubjectId = subjectId;

        return View(
            query
                .OrderByDescending(g => g.Date)
                .ToList());
    }
}