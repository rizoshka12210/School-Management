using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Tests;

public class AttendanceServiceTests
{
    [Fact]
    public async Task GetByStudentAsync_ReturnsOnlyThatStudentsRecords()
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

        var group = new Group { Name = "Group A" };
        var subject = new Subject { Name = "Math" };

        var lesson = new Lesson
        {
            Teacher = teacher,
            Group = group,
            Subject = subject,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        var studentA = new Student { FirstName = "A", LastName = "A", DateOfBirth = DateTime.UtcNow };
        var studentB = new Student { FirstName = "B", LastName = "B", DateOfBirth = DateTime.UtcNow };

        context.Attendances.AddRange(
            new Attendance { Lesson = lesson, Student = studentA, Status = AttendanceStatus.Present },
            new Attendance { Lesson = lesson, Student = studentB, Status = AttendanceStatus.Absent });

        await context.SaveChangesAsync();

        var service = new AttendanceService(context);

        var result = await service.GetByStudentAsync(studentA.Id);

        var record = Assert.Single(result);
        Assert.Equal(AttendanceStatus.Present, record.Status);
    }
}
