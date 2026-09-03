using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

/// <summary>
/// Read-only view of the periodic school-wide Big Exam for the
/// parent's child: their score plus rank within their group and across
/// the whole school for every exam they have a result in.
/// </summary>
public class BigExamController : ParentControllerBase
{
    private readonly BigExamService _bigExamService;

    public BigExamController(
        AppDbContext context,
        OwnershipHelper ownership,
        BigExamService bigExamService)
        : base(context, ownership)
    {
        _bigExamService = bigExamService;
    }

    public async Task<IActionResult> Index(int? studentId)
    {
        var resolvedId = await ResolveStudentIdAsync(studentId);

        if (resolvedId == null)
        {
            return Forbid();
        }

        var student = await Context.Students.FindAsync(resolvedId.Value);

        if (student == null)
        {
            return NotFound();
        }

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        var exams = await _bigExamService.ListAsync();

        var results = new List<BigExamRankingEntry>();

        foreach (var exam in exams)
        {
            results.AddRange(await _bigExamService.GetStudentRankingsAsync(exam.Id, resolvedId.Value));
        }

        return View(results);
    }
}
