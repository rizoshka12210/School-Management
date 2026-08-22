using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Areas.Admin.Controllers;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Tests;

public class AdminScheduleTests
{
    [Fact]
    public async Task Create_AddsValidScheduleEntry()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var controller = CreateController(db);

        var result = await controller.Create(new ScheduleFormViewModel
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var schedule = await db.Schedules.SingleAsync();
        Assert.Equal(DayOfWeek.Monday, schedule.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(10, 0), schedule.EndTime);
    }

    [Fact]
    public async Task Create_RejectsOverlappingTeacherAndGroupSchedule()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        db.Schedules.Add(new Schedule
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.Create(new ScheduleFormViewModel
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(9, 30),
            EndTime = new TimeOnly(10, 30),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Single(await db.Schedules.ToListAsync());
    }

    [Fact]
    public async Task Create_RejectsTeacherNotAssignedToGroupAndSubject()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var otherSubject = new Subject { Name = "Biology" };
        db.Subjects.Add(otherSubject);
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.Create(new ScheduleFormViewModel
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(12, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = otherSubject.Id
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await db.Schedules.ToListAsync());
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesScheduleEntry()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var data = await SeedTeacherAssignmentAsync(db);
        var schedule = new Schedule
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            GroupId = data.Group.Id,
            TeacherId = data.Teacher.Id,
            SubjectId = data.Subject.Id
        };
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.DeleteConfirmed(schedule.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await db.Schedules.ToListAsync());
    }

    private static ScheduleController CreateController(AppDbContext db)
    {
        return ControllerTestHelpers.WithTempData(new ScheduleController(
            db,
            new TestStringLocalizer<SharedResource>()));
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
                UserName = "teacher@test.com",
                Email = "teacher@test.com",
                FullName = "Test Teacher"
            }
        };

        teacher.Groups.Add(group);
        teacher.Subjects.Add(subject);
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();

        return (group, subject, teacher);
    }
}
