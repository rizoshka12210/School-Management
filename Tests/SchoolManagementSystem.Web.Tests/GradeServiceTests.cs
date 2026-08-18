using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Tests;

public class GradeServiceTests
{
    [Fact]
    public async Task GetByStudentAsync_ReturnsOnlyThatStudentsGrades_OrderedByDateDescending()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var teacher = new Teacher
        {
            HourlyRate = 50,
            ApplicationUser = new ApplicationUser
            {
                UserName = "t@test.com",
                Email = "t@test.com",
                FullName = "Teacher"
            }
        };

        var subject = new Subject { Name = "Math" };

        var studentA = new Student { FirstName = "A", LastName = "A", DateOfBirth = DateTime.UtcNow };
        var studentB = new Student { FirstName = "B", LastName = "B", DateOfBirth = DateTime.UtcNow };

        context.Grades.AddRange(
            new Grade
            {
                Student = studentA,
                Subject = subject,
                Teacher = teacher,
                Value = 3,
                Date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Grade
            {
                Student = studentA,
                Subject = subject,
                Teacher = teacher,
                Value = 5,
                Date = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Grade
            {
                Student = studentB,
                Subject = subject,
                Teacher = teacher,
                Value = 4,
                Date = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();

        var service = new GradeService(context);

        var result = await service.GetByStudentAsync(studentA.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(5, result[0].Value);
        Assert.Equal(3, result[1].Value);
    }
}
