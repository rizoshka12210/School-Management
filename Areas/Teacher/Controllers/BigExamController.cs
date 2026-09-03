using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

/// <summary>
/// Grading for the periodic school-wide Big Exam, available only to
/// the single teacher the admin has designated
/// (Teacher.IsBigExamGrader) - every action here checks that flag
/// itself since it cuts across the usual "teacher owns this group"
/// rule (the designated grader can grade every group, not just their
/// own classes).
/// </summary>
public class BigExamController : TeacherControllerBase
{
    private readonly BigExamService _bigExamService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BigExamController(
        AppDbContext context,
        OwnershipHelper ownership,
        BigExamService bigExamService,
        IStringLocalizer<SharedResource> localizer)
        : base(context, ownership)
    {
        _bigExamService = bigExamService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        if (!await Ownership.IsCurrentUserBigExamGraderAsync(User))
        {
            return Forbid();
        }

        var exams = await _bigExamService.ListAsync();

        return View(exams);
    }

    [HttpGet]
    public async Task<IActionResult> Groups(int examId)
    {
        if (!await Ownership.IsCurrentUserBigExamGraderAsync(User))
        {
            return Forbid();
        }

        var exam = await _bigExamService.GetAsync(examId);

        if (exam == null)
        {
            return NotFound();
        }

        ViewBag.Exam = exam;

        var groups = await Context.Groups
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Group(int examId, int groupId)
    {
        if (!await Ownership.IsCurrentUserBigExamGraderAsync(User))
        {
            return Forbid();
        }

        var model = await _bigExamService.BuildSheetAsync(examId, groupId);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Group(BigExamSheetSaveViewModel model)
    {
        if (!await Ownership.IsCurrentUserBigExamGraderAsync(User))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = _localizer["The grade sheet contains invalid values."].Value;

            return RedirectToAction(
                nameof(Group),
                new { examId = model.BigExamId, groupId = model.GroupId });
        }

        var teacherId = await GetTeacherIdAsync();

        var saved = await _bigExamService.SaveSheetAsync(
            model.BigExamId,
            model.GroupId,
            teacherId,
            model.Rows);

        if (!saved)
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["Grades saved."].Value;

        return RedirectToAction(
            nameof(Group),
            new { examId = model.BigExamId, groupId = model.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Rankings(int examId)
    {
        if (!await Ownership.IsCurrentUserBigExamGraderAsync(User))
        {
            return Forbid();
        }

        var exam = await _bigExamService.GetAsync(examId);

        if (exam == null)
        {
            return NotFound();
        }

        ViewBag.Exam = exam;

        var rankings = await _bigExamService.GetRankingsAsync(examId);

        return View(rankings);
    }
}
