using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Areas.Admin.Controllers;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Tests;

public class AdminIdentityCrudTests
{
    [Fact]
    public async Task Parent_CreateEditDelete_UpdatesIdentityAndEntity()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(Roles.Parent));

        var controller = ControllerTestHelpers.WithTempData(new ParentsController(
            db,
            userManager,
            new TestStringLocalizer<SharedResource>()));

        var createResult = await controller.Create(new ParentFormViewModel
        {
            FullName = "Parent One",
            Email = "parent1@test.com",
            Password = "Parent123!"
        });

        Assert.IsType<RedirectToActionResult>(createResult);
        var parent = await db.Parents.Include(p => p.ApplicationUser).SingleAsync();
        Assert.Equal("Parent One", parent.ApplicationUser.FullName);
        Assert.True(await userManager.IsInRoleAsync(parent.ApplicationUser, Roles.Parent));

        var editResult = await controller.Edit(new ParentFormViewModel
        {
            Id = parent.Id,
            FullName = "Parent Updated",
            Email = "parent2@test.com",
            Password = "Updated123!"
        });

        Assert.IsType<RedirectToActionResult>(editResult);
        var updatedUser = await userManager.FindByEmailAsync("parent2@test.com");
        Assert.NotNull(updatedUser);
        Assert.Equal("Parent Updated", updatedUser!.FullName);
        Assert.True(await userManager.CheckPasswordAsync(updatedUser, "Updated123!"));

        var deleteResult = await controller.DeleteConfirmed(parent.Id);

        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Empty(await db.Parents.ToListAsync());
        Assert.Null(await userManager.FindByEmailAsync("parent2@test.com"));
    }

    [Fact]
    public async Task Teacher_CreateEditDelete_UpdatesRelationsSalaryAndIdentity()
    {
        using var provider = TestServiceProviderFactory.Create();
        var db = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(Roles.Teacher));

        var group = new Group { Name = "12-A" };
        var subject = new Subject { Name = "Chemistry" };
        db.AddRange(group, subject);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.WithTempData(new TeachersController(
            db,
            userManager,
            new TestStringLocalizer<SharedResource>(),
            new ActivityLogService(db)));

        var createResult = await controller.Create(new TeacherFormViewModel
        {
            FullName = "Teacher One",
            Email = "teacher1@test.com",
            Password = "Teacher123!",
            HourlyRate = 40,
            GroupIds = new List<int> { group.Id },
            SubjectIds = new List<int> { subject.Id }
        });

        Assert.IsType<RedirectToActionResult>(createResult);
        var teacher = await db.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .SingleAsync();
        Assert.Equal(40m, teacher.HourlyRate);
        Assert.Single(teacher.Groups);
        Assert.Single(teacher.Subjects);
        Assert.True(await userManager.IsInRoleAsync(teacher.ApplicationUser, Roles.Teacher));

        var editResult = await controller.Edit(new TeacherFormViewModel
        {
            Id = teacher.Id,
            FullName = "Teacher Updated",
            Email = "teacher2@test.com",
            Password = "Changed123!",
            HourlyRate = 55,
            GroupIds = new List<int> { group.Id },
            SubjectIds = new List<int> { subject.Id }
        });

        Assert.IsType<RedirectToActionResult>(editResult);
        var updated = await db.Teachers.Include(t => t.ApplicationUser).SingleAsync();
        Assert.Equal(55m, updated.HourlyRate);
        Assert.Equal("Teacher Updated", updated.ApplicationUser.FullName);
        Assert.True(await userManager.CheckPasswordAsync(updated.ApplicationUser, "Changed123!"));
        Assert.Single(await db.ActivityLogEntries.ToListAsync());

        var deleteResult = await controller.DeleteConfirmed(teacher.Id);

        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Empty(await db.Teachers.ToListAsync());
        Assert.Null(await userManager.FindByEmailAsync("teacher2@test.com"));
    }
}
