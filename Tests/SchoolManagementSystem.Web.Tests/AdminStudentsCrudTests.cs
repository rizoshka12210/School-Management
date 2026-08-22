using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Areas.Admin.Controllers;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Tests;

public class AdminStudentsCrudTests
{
    [Fact]
    public async Task Create_AddsStudentAndGroupRelation()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var group = new Group { Name = "10-A" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var model = new StudentFormViewModel
        {
            FirstName = "Alex",
            LastName = "Stone",
            DateOfBirth = new DateOnly(2010, 5, 10),
            GroupId = group.Id
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var student = await db.Students.SingleAsync();
        Assert.Equal("Alex", student.FirstName);
        Assert.Equal("Stone", student.LastName);
        Assert.Equal(group.Id, student.GroupId);
        Assert.Single(await db.ActivityLogEntries.ToListAsync());
    }

    [Fact]
    public async Task Edit_UpdatesStudentData()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var student = new Student
        {
            FirstName = "Old",
            LastName = "Name",
            DateOfBirth = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var model = new StudentFormViewModel
        {
            Id = student.Id,
            FirstName = "New",
            LastName = "Student",
            DateOfBirth = new DateOnly(2011, 2, 3)
        };

        var result = await controller.Edit(model);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await db.Students.SingleAsync();
        Assert.Equal("New", updated.FirstName);
        Assert.Equal("Student", updated.LastName);
        Assert.Equal(2011, updated.DateOfBirth.Year);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesStudent()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var student = new Student
        {
            FirstName = "Delete",
            LastName = "Me",
            DateOfBirth = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.DeleteConfirmed(student.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await db.Students.ToListAsync());
    }

    private static StudentsController CreateController(AppDbContext db)
    {
        return ControllerTestHelpers.WithTempData(new StudentsController(
            db,
            new TestStringLocalizer<SharedResource>(),
            new StudentRiskService(),
            new AchievementService(db),
            new ActivityLogService(db)));
    }
}
