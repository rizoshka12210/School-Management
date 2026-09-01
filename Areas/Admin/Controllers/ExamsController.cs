using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class ExamsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ExamSheetService _examSheetService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ExamsController(
        AppDbContext context,
        ExamSheetService examSheetService,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _examSheetService = examSheetService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
        ViewBag.Subjects = await _context.Subjects.OrderBy(s => s.Name).ToListAsync();

        return View();
    }

    public async Task<IActionResult> Sheet(int groupId, int subjectId)
    {
        var model = await _examSheetService.BuildAsync(groupId, subjectId);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sheet(ExamSheetSaveViewModel model)
    {
        var teacherId = await _context.Teachers
            .Where(t =>
                t.Groups.Any(g => g.Id == model.GroupId) &&
                t.Subjects.Any(s => s.Id == model.SubjectId))
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (teacherId == null)
        {
            TempData["Error"] = _localizer["No teacher is assigned to this group and subject yet."].Value;

            return RedirectToAction(
                nameof(Sheet),
                new { groupId = model.GroupId, subjectId = model.SubjectId });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = _localizer["The exam sheet contains invalid values."].Value;

            return RedirectToAction(
                nameof(Sheet),
                new { groupId = model.GroupId, subjectId = model.SubjectId });
        }

        var saved = await _examSheetService.SaveAsync(
            model.GroupId,
            model.SubjectId,
            teacherId.Value,
            model.Rows);

        if (!saved)
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["Exam results saved."].Value;

        return RedirectToAction(
            nameof(Sheet),
            new { groupId = model.GroupId, subjectId = model.SubjectId });
    }
}
