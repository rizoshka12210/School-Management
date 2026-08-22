using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Areas.Admin.Controllers;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Tests;

public class AdminLessonTests
{
    [Fact]
    public async Task Create_AddsValidLesson()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var controller = CreateController(db);

        var result = await controller.Create(new LessonFormViewModel
        {
            StartTime = new DateTime(2026, 9, 1, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 1, 10, 0, 0),
            Topic = " Algebra ",
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var lesson = await db.Lessons.SingleAsync();
        Assert.Equal("Algebra", lesson.Topic);
        Assert.Equal(DateTimeKind.Utc, lesson.StartTime.Kind);
        Assert.Equal(DateTimeKind.Utc, lesson.EndTime.Kind);
    }

    [Fact]
    public async Task Create_RejectsEndTimeBeforeStartTime()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var controller = CreateController(db);

        var result = await controller.Create(new LessonFormViewModel
        {
            StartTime = new DateTime(2026, 9, 1, 10, 0, 0),
            EndTime = new DateTime(2026, 9, 1, 9, 0, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await db.Lessons.ToListAsync());
    }

    [Fact]
    public async Task Create_RejectsTeacherNotAssignedToSubject()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var otherSubject = new Subject { Name = "History" };
        db.Subjects.Add(otherSubject);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.Create(new LessonFormViewModel
        {
            StartTime = new DateTime(2026, 9, 2, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 2, 10, 0, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = otherSubject.Id
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await db.Lessons.ToListAsync());
    }

    [Fact]
    public async Task DeleteConfirmed_IsBlockedWhenAttendanceExists()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var student = new Student
        {
            FirstName = "Test",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Group = data.Group
        };
        var lesson = new Lesson
        {
            StartTime = new DateTime(2026, 9, 3, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc),
            Group = data.Group,
            Teacher = data.Teacher,
            Subject = data.Subject
        };
        db.Students.Add(student);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        db.Attendances.Add(new Attendance
        {
            StudentId = student.Id,
            LessonId = lesson.Id,
            Status = AttendanceStatus.Present
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.DeleteConfirmed(lesson.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Single(await db.Lessons.ToListAsync());
        Assert.True(controller.TempData.ContainsKey("Error"));
    }

    private static LessonsController CreateController(AppDbContext db)
    {
        var controller = new LessonsController(
            db,
            new TestStringLocalizer<SharedResource>());
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            new TestTempDataProvider());
        return controller;
    }

    private static async Task<(Group Group, Subject Subject, Teacher Teacher)> SeedTeacherAssignmentAsync(
        AppDbContext db)
    {
        var group = new Group { Name = "10-A" };
        var subject = new Subject { Name = "Mathematics" };
        var teacher = new Teacher
        {
            HourlyRate = 50,
            ApplicationUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "lesson.teacher@test.com",
                Email = "lesson.teacher@test.com",
                FullName = "Lesson Teacher"
            }
        };

        teacher.Groups.Add(group);
        teacher.Subjects.Add(subject);
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();

        return (group, subject, teacher);
    }
}
