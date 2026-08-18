using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Tests;

/// <summary>
/// These are the IDOR guards every Teacher/Parent controller relies on
/// (see Data/Seed/DbSeeder.cs, which seeds a second teacher/parent
/// specifically so "someone else's data" has a case to probe). A
/// regression here would let one teacher or parent reach another
/// family's records.
/// </summary>
public class OwnershipHelperTests
{
    [Fact]
    public async Task TeacherOwnsLessonAsync_IsTrue_ForOwnLesson_AndFalse_ForAnotherTeachersLesson()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var ownTeacherUser = await CreateUserAsync(userManager, "owner@test.com");
        var otherTeacherUser = await CreateUserAsync(userManager, "other@test.com");

        var ownTeacher = new Teacher
        {
            ApplicationUserId = ownTeacherUser.Id,
            HourlyRate = 50
        };

        var otherTeacher = new Teacher
        {
            ApplicationUserId = otherTeacherUser.Id,
            HourlyRate = 50
        };

        context.Teachers.AddRange(ownTeacher, otherTeacher);

        var ownLesson = new Lesson
        {
            Teacher = ownTeacher,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        var otherLesson = new Lesson
        {
            Teacher = otherTeacher,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        context.Lessons.AddRange(ownLesson, otherLesson);

        await context.SaveChangesAsync();

        var ownership = new OwnershipHelper(context, userManager);
        var principal = BuildPrincipal(ownTeacherUser);

        Assert.True(
            await ownership.TeacherOwnsLessonAsync(principal, ownLesson.Id));

        Assert.False(
            await ownership.TeacherOwnsLessonAsync(principal, otherLesson.Id));
    }

    [Fact]
    public async Task ParentOwnsStudentAsync_IsTrue_ForOwnChild_AndFalse_ForAnotherParentsChild()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var ownParentUser = await CreateUserAsync(userManager, "parent1@test.com");
        var otherParentUser = await CreateUserAsync(userManager, "parent2@test.com");

        var ownParent = new Parent { ApplicationUserId = ownParentUser.Id };
        var otherParent = new Parent { ApplicationUserId = otherParentUser.Id };

        var ownChild = new Student
        {
            FirstName = "Own",
            LastName = "Child",
            DateOfBirth = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var otherChild = new Student
        {
            FirstName = "Other",
            LastName = "Child",
            DateOfBirth = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        ownParent.Students.Add(ownChild);
        otherParent.Students.Add(otherChild);

        context.Parents.AddRange(ownParent, otherParent);
        context.Students.AddRange(ownChild, otherChild);

        await context.SaveChangesAsync();

        var ownership = new OwnershipHelper(context, userManager);
        var principal = BuildPrincipal(ownParentUser);

        Assert.True(
            await ownership.ParentOwnsStudentAsync(principal, ownChild.Id));

        Assert.False(
            await ownership.ParentOwnsStudentAsync(principal, otherChild.Id));
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = email
        };

        var result = await userManager.CreateAsync(user, "P@ssword1!");

        Assert.True(result.Succeeded);

        return user;
    }

    private static ClaimsPrincipal BuildPrincipal(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) },
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
