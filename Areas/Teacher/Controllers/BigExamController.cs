using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

/// <summary>
/// Every teacher can browse the periodic school-wide Big Exam - exam
/// list, sheets and rankings - but only the single teacher the admin
/// has designated (Teacher.IsBigExamGrader) can actually save grades.
/// The designated grader can grade every group, not just their own
/// classes, since the exam is school-wide.
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
        var exams = await _bigExamService.ListAsync();

        return View(exams);
    }

    [HttpGet]
    public async Task<IActionResult> Groups(int examId)
    {
        var exam = await _bigExamService.GetAsync(examId);

        if (exam == null)
        {
            return NotFound();
        }

        ViewBag.Exam = exam;

        var overview = await _bigExamService.GetGroupOverviewAsync(examId);

        return View(overview);
    }

    [HttpGet]
    public async Task<IActionResult> Group(int examId, int groupId)
    {
        var model = await _bigExamService.BuildGroupSheetAsync(examId, groupId);

        if (model == null)
        {
            return NotFound();
        }

        ViewBag.CanEdit = await Ownership.IsCurrentUserBigExamGraderAsync(User);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Group(BigExamGroupSheetSaveViewModel model)
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

        var saved = await _bigExamService.SaveGroupSheetAsync(
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
    public async Task<IActionResult> Rankings(int examId, int? subjectId)
    {
        var exam = await _bigExamService.GetAsync(examId);

        if (exam == null)
        {
            return NotFound();
        }

        ViewBag.Exam = exam;
        ViewBag.Subjects = await _bigExamService.ListSubjectsAsync();
        ViewBag.SubjectId = subjectId;

        var rankings = await _bigExamService.GetRankingsAsync(examId, subjectId);

        return View(rankings);
    }
}
