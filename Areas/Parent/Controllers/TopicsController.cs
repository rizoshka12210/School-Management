using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

public class TopicsController : ParentControllerBase
{
    private readonly TopicCalendarService _topicCalendarService;

    public TopicsController(
        AppDbContext context,
        OwnershipHelper ownership,
        TopicCalendarService topicCalendarService)
        : base(context, ownership)
    {
        _topicCalendarService = topicCalendarService;
    }

    public async Task<IActionResult> Index(int? studentId)
    {
        var resolvedId = await ResolveStudentIdAsync(studentId);

        if (resolvedId == null)
        {
            return Forbid();
        }

        var student = await Context.Students
            .FirstOrDefaultAsync(s => s.Id == resolvedId);

        if (student == null)
        {
            return NotFound();
        }

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        var lessons = await _topicCalendarService.GetStudentTopicsAsync(
            resolvedId.Value);

        return View(lessons);
    }
}
