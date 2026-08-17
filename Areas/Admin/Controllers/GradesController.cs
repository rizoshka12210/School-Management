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

    public GradesController(GradeService gradeService)
    {
        _gradeService = gradeService;
    }

    public async Task<IActionResult> Index()
    {
        var grades = await _gradeService.GetAllAsync();

        return View(grades);
    }
}