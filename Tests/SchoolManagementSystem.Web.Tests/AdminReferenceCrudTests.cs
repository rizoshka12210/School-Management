using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Areas.Admin.Controllers;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Tests;

public class AdminReferenceCrudTests
{
    [Fact]
    public async Task Groups_CreateEditDelete_Works()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var controller = new GroupsController(
            db,
            new TestStringLocalizer<SharedResource>());

        var createResult = await controller.Create(new GroupFormViewModel
        {
            Name = " 11-A "
        });

        Assert.IsType<RedirectToActionResult>(createResult);
        var group = await db.Groups.SingleAsync();
        Assert.Equal("11-A", group.Name);

        var editResult = await controller.Edit(new GroupFormViewModel
        {
            Id = group.Id,
            Name = "11-B"
        });

        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("11-B", (await db.Groups.SingleAsync()).Name);

        var deleteResult = await controller.DeleteConfirmed(group.Id);

        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Empty(await db.Groups.ToListAsync());
    }

    [Fact]
    public async Task Groups_DuplicateName_IsRejected()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        db.Groups.Add(new Group { Name = "9-A" });
        await db.SaveChangesAsync();

        var controller = new GroupsController(
            db,
            new TestStringLocalizer<SharedResource>());

        var result = await controller.Create(new GroupFormViewModel
        {
            Name = "9-a"
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Single(await db.Groups.ToListAsync());
    }

    [Fact]
    public async Task Subjects_CreateEditDelete_Works()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var controller = new SubjectsController(
            db,
            new TestStringLocalizer<SharedResource>());

        var createResult = await controller.Create(new SubjectFormViewModel
        {
            Name = " Physics "
        });

        Assert.IsType<RedirectToActionResult>(createResult);
        var subject = await db.Subjects.SingleAsync();
        Assert.Equal("Physics", subject.Name);

        var editResult = await controller.Edit(new SubjectFormViewModel
        {
            Id = subject.Id,
            Name = "Advanced Physics"
        });

        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Advanced Physics", (await db.Subjects.SingleAsync()).Name);

        var deleteResult = await controller.DeleteConfirmed(subject.Id);

        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Empty(await db.Subjects.ToListAsync());
    }

    [Fact]
    public async Task Subjects_DuplicateName_IsRejected()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        db.Subjects.Add(new Subject { Name = "Mathematics" });
        await db.SaveChangesAsync();

        var controller = new SubjectsController(
            db,
            new TestStringLocalizer<SharedResource>());

        var result = await controller.Create(new SubjectFormViewModel
        {
            Name = "mathematics"
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Single(await db.Subjects.ToListAsync());
    }
}
