using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Teacher;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class TopicsController : TeacherControllerBase
{
    private readonly TopicCalendarService _topicCalendarService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TopicsController(
        AppDbContext context,
        OwnershipHelper ownership,
        TopicCalendarService topicCalendarService,
        IStringLocalizer<SharedResource> localizer)
        : base(context, ownership)
    {
        _topicCalendarService = topicCalendarService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var lessons = await _topicCalendarService.GetTeacherTopicsAsync(
            teacherId.Value);

        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        var model = new TopicEditViewModel
        {
            LessonId = lesson.Id,
            GroupName = lesson.Group.Name,
            SubjectName = lesson.Subject.Name,
            LessonDate = lesson.StartTime,
            Topic = lesson.Topic
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TopicEditViewModel model)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(
            User,
            model.LessonId);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.GroupName = lesson.Group.Name;
            model.SubjectName = lesson.Subject.Name;
            model.LessonDate = lesson.StartTime;

            return View(model);
        }

        lesson.Topic = string.IsNullOrWhiteSpace(model.Topic)
            ? null
            : model.Topic.Trim();

        await Context.SaveChangesAsync();

        TempData["Success"] = _localizer["Topic saved."];

        return RedirectToAction(nameof(Index));
    }
}
