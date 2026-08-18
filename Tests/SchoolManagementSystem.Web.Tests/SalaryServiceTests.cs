using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Tests;

public class SalaryServiceTests
{
    [Fact]
    public async Task CalculateAsync_SumsOnlyLessonsWithinTheGivenMonth()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var teacher = new Teacher
        {
            HourlyRate = 50,
            ApplicationUser = new ApplicationUser
            {
                UserName = "teacher@test.com",
                Email = "teacher@test.com",
                FullName = "Test Teacher"
            }
        };

        context.Teachers.Add(teacher);

        // Two one-hour lessons in August (counted) and one in July
        // (must be excluded by the month filter).
        context.Lessons.AddRange(
            new Lesson
            {
                Teacher = teacher,
                StartTime = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc)
            },
            new Lesson
            {
                Teacher = teacher,
                StartTime = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 8, 10, 10, 30, 0, DateTimeKind.Utc)
            },
            new Lesson
            {
                Teacher = teacher,
                StartTime = new DateTime(2026, 7, 31, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 7, 31, 11, 0, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();

        var salaryService = new SalaryService(context);

        var summary = await salaryService.CalculateAsync(teacher.Id, 2026, 8);

        Assert.NotNull(summary);
        Assert.Equal(2, summary!.LessonsCount);
        Assert.Equal(2.5m, summary.WorkedHours);
        Assert.Equal(125m, summary.TotalSalary);
    }

    [Fact]
    public async Task CalculateAsync_ReturnsNull_WhenTeacherDoesNotExist()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var salaryService = new SalaryService(context);

        var summary = await salaryService.CalculateAsync(999, 2026, 8);

        Assert.Null(summary);
    }
}
