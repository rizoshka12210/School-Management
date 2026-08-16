using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.ViewModels.Teacher;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class TopicsController : TeacherControllerBase
{
    public TopicsController(
        AppDbContext context,
        OwnershipHelper ownership)
        : base(context, ownership)
    {
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = await GetTeacherIdAsync();

        if (teacherId == null)
        {
            return Forbid();
        }

        var lessons = await Context.Lessons
            .Where(l => l.TeacherId == teacherId)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

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

        TempData["Success"] = "Topic saved.";

        return RedirectToAction(nameof(Index));
    }
}
