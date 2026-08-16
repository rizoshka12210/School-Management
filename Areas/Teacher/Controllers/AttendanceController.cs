using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.ViewModels.Teacher;

using AttendanceEntity = SchoolManagementSystem.Web.Models.Entities.Attendance;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class AttendanceController : TeacherControllerBase
{
    public AttendanceController(
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
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Mark(int lessonId)
    {
        var owns = await Ownership.TeacherOwnsLessonAsync(User, lessonId);

        if (!owns)
        {
            return Forbid();
        }

        var lesson = await Context.Lessons
            .Include(l => l.Group)
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var model = new AttendanceMarkViewModel
        {
            LessonId = lesson.Id,
            GroupName = lesson.Group.Name,
            SubjectName = lesson.Subject.Name,
            LessonDate = lesson.StartTime,

            Students = lesson.Group.Students
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Select(s =>
                {
                    var existing = lesson.Attendances
                        .FirstOrDefault(a => a.StudentId == s.Id);

                    return new AttendanceRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        Status = existing?.Status ?? AttendanceStatus.Present
                    };
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(AttendanceMarkViewModel model)
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
                .ThenInclude(g => g.Students)
            .Include(l => l.Attendances)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return NotFound();
        }

        var validStudentIds = lesson.Group.Students
            .Select(s => s.Id)
            .ToHashSet();

        foreach (var row in model.Students)
        {
            // A student not in this lesson's group cannot receive
            // attendance for it, even if the posted form was altered.
            if (!validStudentIds.Contains(row.StudentId))
            {
                continue;
            }

            var existing = lesson.Attendances
                .FirstOrDefault(a => a.StudentId == row.StudentId);

            if (existing != null)
            {
                existing.Status = row.Status;
            }
            else
            {
                Context.Attendances.Add(new AttendanceEntity
                {
                    LessonId = lesson.Id,
                    StudentId = row.StudentId,
                    Status = row.Status
                });
            }
        }

        await Context.SaveChangesAsync();

        TempData["Success"] = "Attendance saved.";

        return RedirectToAction(
            nameof(Mark),
            new { lessonId = lesson.Id });
    }
}
