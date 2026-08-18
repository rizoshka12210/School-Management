using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class AttendanceController : Controller
{
    private readonly AttendanceService _attendanceService;
    private readonly AppDbContext _context;

    public AttendanceController(
        AttendanceService attendanceService,
        AppDbContext context)
    {
        _attendanceService = attendanceService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? date)
    {
        var attendances =
            await _attendanceService.GetAllAsync();

        var query = attendances.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(a =>
                $"{a.Student.FirstName} {a.Student.LastName}"
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Group.Name
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Subject.Name
                    .ToLower()
                    .Contains(value) ||
                a.Lesson.Teacher.ApplicationUser.FullName
                    .ToLower()
                    .Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AttendanceStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(a =>
                a.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(date) &&
            DateOnly.TryParse(
                date,
                out var parsedDate))
        {
            query = query.Where(a =>
                DateOnly.FromDateTime(
                    a.Lesson.StartTime) == parsedDate);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Date = date;

        return View(
            query
                .OrderByDescending(
                    a => a.Lesson.StartTime)
                .ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Journal(
        int? groupId,
        int? subjectId,
        DateTime? from,
        DateTime? to)
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.Subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (!groupId.HasValue)
        {
            ViewBag.GroupId = null;
            ViewBag.SubjectId = subjectId;

            return View();
        }

        var fromUtc = DateTime.SpecifyKind(
            (from ?? DateTime.UtcNow.AddMonths(-1)).Date,
            DateTimeKind.Utc);

        var toUtc = DateTime.SpecifyKind(
            (to ?? DateTime.UtcNow).Date.AddDays(1),
            DateTimeKind.Utc);

        var journal = await _attendanceService.BuildJournalAsync(
            groupId.Value,
            fromUtc,
            toUtc,
            subjectId);

        ViewBag.GroupId = groupId;
        ViewBag.SubjectId = subjectId;
        ViewBag.From = fromUtc;
        ViewBag.To = toUtc;

        return View(journal);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return NotFound();
        }

        var model = new AttendanceFormViewModel
        {
            Id = attendance.Id,
            StudentName =
                $"{attendance.Student.FirstName} {attendance.Student.LastName}",
            GroupName = attendance.Lesson.Group.Name,
            SubjectName = attendance.Lesson.Subject.Name,
            LessonDate = attendance.Lesson.StartTime,
            Status = attendance.Status,
            Note = attendance.Note
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AttendanceFormViewModel model)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        if (attendance == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.StudentName =
                $"{attendance.Student.FirstName} {attendance.Student.LastName}";
            model.GroupName = attendance.Lesson.Group.Name;
            model.SubjectName = attendance.Lesson.Subject.Name;
            model.LessonDate = attendance.Lesson.StartTime;

            return View(model);
        }

        attendance.Status = model.Status;
        attendance.Note = string.IsNullOrWhiteSpace(model.Note)
            ? null
            : model.Note.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = "Attendance record updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return NotFound();
        }

        return View(attendance);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return NotFound();
        }

        _context.Attendances.Remove(attendance);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Attendance record deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}