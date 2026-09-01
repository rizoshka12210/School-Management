using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class ExamsController : TeacherControllerBase
{
    private readonly ExamSheetService _examSheetService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ExamsController(
        AppDbContext context,
        OwnershipHelper ownership,
        ExamSheetService examSheetService,
        IStringLocalizer<SharedResource> localizer)
        : base(context, ownership)
    {
        _examSheetService = examSheetService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var teacher = await Context.Teachers
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstAsync(t => t.Id == teacherId);

        ViewBag.Groups = teacher.Groups.OrderBy(g => g.Name).ToList();
        ViewBag.Subjects = teacher.Subjects.OrderBy(s => s.Name).ToList();

        return View();
    }

    public async Task<IActionResult> Sheet(int groupId, int subjectId)
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        if (!await TeacherMatchesAsync(teacherId.Value, groupId, subjectId))
        {
            return Forbid();
        }

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
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        if (!await TeacherMatchesAsync(teacherId.Value, model.GroupId, model.SubjectId))
        {
            return Forbid();
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

    private async Task<bool> TeacherMatchesAsync(int teacherId, int groupId, int subjectId)
    {
        return await Context.Teachers
            .AnyAsync(t =>
                t.Id == teacherId &&
                t.Groups.Any(g => g.Id == groupId) &&
                t.Subjects.Any(s => s.Id == subjectId));
    }
}
