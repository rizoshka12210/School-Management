using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

public class StudentsController : TeacherControllerBase
{
    public StudentsController(
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

        var students = await Context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .SelectMany(g => g.Students)
            .Include(s => s.Group)
            .Distinct()
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();

        return View(students);
    }

    public async Task<IActionResult> Details(int id)
    {
        var owns = await Ownership.TeacherOwnsStudentAsync(User, id);

        if (!owns)
        {
            return Forbid();
        }

        var teacherId = await GetTeacherIdAsync();

        var student = await Context.Students
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        var attendances = await Context.Attendances
            .Where(a =>
                a.StudentId == id &&
                a.Lesson.TeacherId == teacherId)
            .ToListAsync();

        var totalLessons = attendances.Count;

        var presentCount = attendances
            .Count(a => a.Status == AttendanceStatus.Present);

        ViewBag.TotalLessons = totalLessons;
        ViewBag.PresentCount = presentCount;

        ViewBag.AttendanceRate = totalLessons == 0
            ? 0
            : Math.Round(presentCount * 100.0 / totalLessons, 1);

        var grades = await Context.Grades
            .Where(g =>
                g.StudentId == id &&
                g.TeacherId == teacherId)
            .Include(g => g.Subject)
            .OrderByDescending(g => g.Date)
            .ToListAsync();

        var examGrades = await Context.ExamGrades
            .Where(e =>
                e.StudentId == id &&
                e.TeacherId == teacherId)
            .ToListAsync();

        ViewBag.Grades = grades;

        ViewBag.AverageGrade = GradeAveragingHelper.Combine(
            grades.Select(g => g.Value),
            examGrades.Select(e => e.Average)) ?? 0;

        return View(student);
    }
}
