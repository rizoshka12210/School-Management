using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;
using SchoolManagementSystem.Web.ViewModels.Shared;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

/// <summary>
/// The periodic school-wide Big Exam (held roughly every two weeks),
/// separate from the regular per-subject exam sheets. Viewing the exam
/// list and rankings is open to Admin and Director; creating/editing/
/// deleting exam sessions and entering grades is Admin-only here (the
/// one teacher the admin designates grades through the Teacher area
/// instead - see Areas/Teacher/Controllers/BigExamController.cs).
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class BigExamController : Controller
{
    private readonly BigExamService _bigExamService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BigExamController(
        BigExamService bigExamService,
        IStringLocalizer<SharedResource> localizer)
    {
        _bigExamService = bigExamService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var exams = await _bigExamService.ListAsync();

        ViewBag.Grader = await _bigExamService.GetCurrentGraderAsync();

        return View(exams);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create()
    {
        return View(new BigExamFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(BigExamFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _bigExamService.CreateAsync(model.Title, model.Date);

        TempData["Success"] = _localizer["Big Exam created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _bigExamService.GetAsync(id);

        if (exam == null)
        {
            return NotFound();
        }

        return View(new BigExamFormViewModel
        {
            Id = exam.Id,
            Title = exam.Title,
            Date = exam.Date
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(BigExamFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _bigExamService.UpdateAsync(model.Id, model.Title, model.Date);

        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["Big Exam updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _bigExamService.GetAsync(id);

        if (exam == null)
        {
            return NotFound();
        }

        return View(exam);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _bigExamService.DeleteAsync(id);

        TempData["Success"] = _localizer["Big Exam deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Group(int examId, int groupId, int subjectId)
    {
        var model = await _bigExamService.BuildSheetAsync(examId, groupId, subjectId);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Group(BigExamSheetSaveViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = _localizer["The grade sheet contains invalid values."].Value;

            return RedirectToAction(
                nameof(Group),
                new { examId = model.BigExamId, groupId = model.GroupId, subjectId = model.SubjectId });
        }

        var saved = await _bigExamService.SaveSheetAsync(
            model.BigExamId,
            model.GroupId,
            model.SubjectId,
            teacherId: null,
            model.Rows);

        if (!saved)
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["Grades saved."].Value;

        return RedirectToAction(
            nameof(Group),
            new { examId = model.BigExamId, groupId = model.GroupId, subjectId = model.SubjectId });
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

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GraderAccess()
    {
        ViewBag.Teachers = await _bigExamService.ListTeachersForGraderAsync();

        var grader = await _bigExamService.GetCurrentGraderAsync();

        return View(grader?.Id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GraderAccess(int? teacherId)
    {
        await _bigExamService.SetGraderAsync(teacherId);

        TempData["Success"] = _localizer["Big Exam grader access updated."].Value;

        return RedirectToAction(nameof(GraderAccess));
    }
}
