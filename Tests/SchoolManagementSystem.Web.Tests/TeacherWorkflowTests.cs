using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Teacher;
using TeacherAttendanceController = SchoolManagementSystem.Web.Areas.Teacher.Controllers.AttendanceController;
using TeacherGradesController = SchoolManagementSystem.Web.Areas.Teacher.Controllers.GradesController;

namespace SchoolManagementSystem.Web.Tests;

public class TeacherWorkflowTests
{
    [Fact]
    public async Task MarkAttendance_SavesStatusForOwnedLesson()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var data = await SeedTeacherLessonAsync(db, userManager);
        var ownership = new OwnershipHelper(db, userManager);

        var controller = ControllerTestHelpers.WithTempData(new TeacherAttendanceController(
            db,
            ownership,
            new AttendanceService(db),
            new TestStringLocalizer<SharedResource>(),
            new ActivityLogService(db)));
        SetUser(controller, data.User);

        var result = await controller.Mark(new AttendanceMarkViewModel
        {
            LessonId = data.Lesson.Id,
            Students = new List<AttendanceRow>
            {
                new()
                {
                    StudentId = data.Student.Id,
                    StudentName = "Test Student",
                    Status = AttendanceStatus.Absent
                }
            }
        });

        Assert.IsType<RedirectToActionResult>(result);
        var attendance = await db.Attendances.SingleAsync();
        Assert.Equal(data.Student.Id, attendance.StudentId);
        Assert.Equal(AttendanceStatus.Absent, attendance.Status);
        Assert.Single(await db.ActivityLogEntries.ToListAsync());
    }

    [Fact]
    public async Task MarkAttendance_UpdatesExistingRecordInsteadOfDuplicating()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var data = await SeedTeacherLessonAsync(db, userManager);
        db.Attendances.Add(new Attendance
        {
            LessonId = data.Lesson.Id,
            StudentId = data.Student.Id,
            Status = AttendanceStatus.Present
        });
        await db.SaveChangesAsync();
        var ownership = new OwnershipHelper(db, userManager);

        var controller = ControllerTestHelpers.WithTempData(new TeacherAttendanceController(
            db,
            ownership,
            new AttendanceService(db),
            new TestStringLocalizer<SharedResource>(),
            new ActivityLogService(db)));
        SetUser(controller, data.User);

        await controller.Mark(new AttendanceMarkViewModel
        {
            LessonId = data.Lesson.Id,
            Students = new List<AttendanceRow>
            {
                new()
                {
                    StudentId = data.Student.Id,
                    StudentName = "Test Student",
                    Status = AttendanceStatus.Late
                }
            }
        });

        var records = await db.Attendances.ToListAsync();
        Assert.Single(records);
        Assert.Equal(AttendanceStatus.Late, records[0].Status);
    }

    [Fact]
    public async Task AddGrades_SavesAndUpdatesGradeForOwnedLesson()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var data = await SeedTeacherLessonAsync(db, userManager);
        var ownership = new OwnershipHelper(db, userManager);

        var controller = ControllerTestHelpers.WithTempData(new TeacherGradesController(
            db,
            ownership,
            new TestStringLocalizer<SharedResource>(),
            new ActivityLogService(db)));
        SetUser(controller, data.User);

        var firstResult = await controller.Add(new GradeEntryViewModel
        {
            LessonId = data.Lesson.Id,
            Students = new List<GradeRow>
            {
                new()
                {
                    StudentId = data.Student.Id,
                    StudentName = "Test Student",
                    Value = 4,
                    Comment = "Good"
                }
            }
        });

        Assert.IsType<RedirectToActionResult>(firstResult);
        var grade = await db.Grades.SingleAsync();
        Assert.Equal(4, grade.Value);
        Assert.Equal("Good", grade.Comment);

        await controller.Add(new GradeEntryViewModel
        {
            LessonId = data.Lesson.Id,
            Students = new List<GradeRow>
            {
                new()
                {
                    StudentId = data.Student.Id,
                    StudentName = "Test Student",
                    Value = 5,
                    Comment = "Excellent"
                }
            }
        });

        var grades = await db.Grades.ToListAsync();
        Assert.Single(grades);
        Assert.Equal(5, grades[0].Value);
        Assert.Equal("Excellent", grades[0].Comment);
    }

    [Fact]
    public async Task TeacherCannotWriteToAnotherTeachersLesson()
    {
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var owner = await SeedTeacherLessonAsync(db, userManager, "owner@test.com");
        var other = await SeedTeacherLessonAsync(db, userManager, "other@test.com");
        var ownership = new OwnershipHelper(db, userManager);

        var controller = ControllerTestHelpers.WithTempData(new TeacherAttendanceController(
            db,
            ownership,
            new AttendanceService(db),
            new TestStringLocalizer<SharedResource>(),
            new ActivityLogService(db)));
        SetUser(controller, owner.User);

        var result = await controller.Mark(new AttendanceMarkViewModel
        {
            LessonId = other.Lesson.Id,
            Students = new List<AttendanceRow>()
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await db.Attendances.ToListAsync());
    }

    private static void SetUser(Controller controller, ApplicationUser user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) },
                    "Test"))
            }
        };
    }

    private static async Task<(ApplicationUser User, Teacher Teacher, Student Student, Lesson Lesson)> SeedTeacherLessonAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        string email = "teacher.workflow@test.com")
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = email
        };
        var userResult = await userManager.CreateAsync(user, "P@ssword1!");
        Assert.True(userResult.Succeeded);

        var group = new Group { Name = Guid.NewGuid().ToString() };
        var subject = new Subject { Name = Guid.NewGuid().ToString() };
        var teacher = new Teacher
        {
            ApplicationUserId = user.Id,
            ApplicationUser = user,
            HourlyRate = 50
        };
        teacher.Groups.Add(group);
        teacher.Subjects.Add(subject);

        var student = new Student
        {
            FirstName = "Test",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Group = group
        };
        var lesson = new Lesson
        {
            StartTime = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            Group = group,
            Subject = subject,
            Teacher = teacher
        };

        db.Teachers.Add(teacher);
        db.Students.Add(student);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        return (user, teacher, student, lesson);
    }
}
