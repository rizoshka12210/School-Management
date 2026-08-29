using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext =
            serviceProvider.GetRequiredService<Data.AppDbContext>();


        string[] roles =
        {
            Roles.Admin,
            Roles.Teacher,
            Roles.Parent,
            Roles.Director
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role)
                );
            }
        }


        var adminEmail = "admin@school.com";

        var adminUser =
            await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                adminUser,
                "Admin123!"
            );

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    adminUser,
                    Roles.Admin
                );
            }
        }


        var directorEmail = "director@school.com";

        var directorUser =
            await userManager.FindByEmailAsync(directorEmail);

        if (directorUser == null)
        {
            directorUser = new ApplicationUser
            {
                UserName = directorEmail,
                Email = directorEmail,
                FullName = "School Director",
                EmailConfirmed = true
            };

            var directorResult = await userManager.CreateAsync(
                directorUser,
                "Director123!"
            );

            if (directorResult.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    directorUser,
                    Roles.Director
                );
            }
        }


        var teacherEmail = "teacher@school.com";

        var teacherUser =
            await userManager.FindByEmailAsync(teacherEmail);

        if (teacherUser == null)
        {
            teacherUser = new ApplicationUser
            {
                UserName = teacherEmail,
                Email = teacherEmail,
                FullName = "Test Teacher",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                teacherUser,
                "Teacher123!"
            );

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    teacherUser,
                    Roles.Teacher
                );
            }
        }

        if (teacherUser != null)
        {
            var teacherExists =
                await dbContext.Teachers
                    .AnyAsync(t =>
                        t.ApplicationUserId == teacherUser.Id);

            if (!teacherExists)
            {
                dbContext.Teachers.Add(
                    new Teacher
                    {
                        ApplicationUserId = teacherUser.Id,
                        HourlyRate = 50
                    }
                );
            }
        }


        var parentEmail = "parent@school.com";

        var parentUser =
            await userManager.FindByEmailAsync(parentEmail);

        if (parentUser == null)
        {
            parentUser = new ApplicationUser
            {
                UserName = parentEmail,
                Email = parentEmail,
                FullName = "Test Parent",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                parentUser,
                "Parent123!"
            );

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    parentUser,
                    Roles.Parent
                );
            }
        }

        if (parentUser != null)
        {
            var parentExists =
                await dbContext.Parents
                    .AnyAsync(p =>
                        p.ApplicationUserId == parentUser.Id);

            if (!parentExists)
            {
                dbContext.Parents.Add(
                    new Parent
                    {
                        ApplicationUserId = parentUser.Id
                    }
                );
            }
        }

        await dbContext.SaveChangesAsync();

        await SeedDemoDataAsync(dbContext, userManager, teacherUser, parentUser);
    }

    /// <summary>
    /// Populates the database with a second teacher/parent (needed so the
    /// Teacher/Parent security checklist has a "someone else's data" case
    /// to probe for IDOR) plus groups, subjects, students and a month's
    /// worth of already-conducted lessons with attendance and grades, so
    /// the Teacher/Parent screens are not empty on first run. Skipped once
    /// any Group exists, so it only ever runs once per database.
    /// </summary>
    private static async Task SeedDemoDataAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ApplicationUser? teacherUser,
        ApplicationUser? parentUser)
    {
        if (await dbContext.Groups.AnyAsync())
        {
            return;
        }

        if (teacherUser == null || parentUser == null)
        {
            return;
        }

        var teacher1 = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == teacherUser.Id);

        var parent1 = await dbContext.Parents
            .FirstOrDefaultAsync(p => p.ApplicationUserId == parentUser.Id);

        if (teacher1 == null || parent1 == null)
        {
            return;
        }

        var teacher2User = await GetOrCreateDemoUserAsync(
            userManager,
            "teacher2@school.com",
            "Second Teacher",
            "Teacher123!",
            Roles.Teacher);

        var parent2User = await GetOrCreateDemoUserAsync(
            userManager,
            "parent2@school.com",
            "Second Parent",
            "Parent123!",
            Roles.Parent);

        if (teacher2User == null || parent2User == null)
        {
            return;
        }

        var teacher2 = new Teacher
        {
            ApplicationUserId = teacher2User.Id,
            HourlyRate = 60
        };

        var parent2 = new Parent
        {
            ApplicationUserId = parent2User.Id
        };

        var math = new Subject { Name = "Mathematics" };
        var english = new Subject { Name = "English" };
        var physics = new Subject { Name = "Physics" };
        var chemistry = new Subject { Name = "Chemistry" };

        var groupA = new Group { Name = "Group 5A" };
        var groupB = new Group { Name = "Group 6B" };
        var groupC = new Group { Name = "Group 7C" };

        teacher1.Groups.Add(groupA);
        teacher1.Groups.Add(groupC);
        teacher1.Subjects.Add(math);
        teacher1.Subjects.Add(english);

        teacher2.Groups.Add(groupB);
        teacher2.Subjects.Add(physics);
        teacher2.Subjects.Add(chemistry);

        Student NewStudent(
            string firstName,
            string lastName,
            int year,
            int month,
            int day,
            Group group)
        {
            return new Student
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateTime(
                    year, month, day, 0, 0, 0, DateTimeKind.Utc),
                Group = group
            };
        }

        var alice = NewStudent("Alice", "Johnson", 2015, 3, 12, groupA);
        var bob = NewStudent("Bob", "Smith", 2015, 7, 4, groupA);
        var carol = NewStudent("Carol", "White", 2015, 1, 20, groupA);

        var david = NewStudent("David", "Brown", 2014, 5, 9, groupB);
        var eva = NewStudent("Eva", "Green", 2014, 9, 17, groupB);
        var frank = NewStudent("Frank", "Black", 2014, 11, 2, groupB);

        var grace = NewStudent("Grace", "Lee", 2013, 2, 28, groupC);
        var henry = NewStudent("Henry", "Adams", 2013, 6, 15, groupC);

        // parent1 has two children in two different groups taught by
        // teacher1, to exercise multi-child support on the Parent
        // dashboard. parent2's child (David) belongs to teacher2's group,
        // so it is the "someone else's data" case for IDOR testing.
        parent1.Students.Add(alice);
        parent1.Students.Add(grace);
        parent2.Students.Add(david);

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(
            today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var lessonPlans = new (
            int Day,
            int Hour,
            int DurationMin,
            Group Group,
            Teacher Teacher,
            Subject Subject,
            string Topic,
            Student[] Students)[]
        {
            (3, 9, 60, groupA, teacher1, math, "Algebra basics", new[] { alice, bob, carol }),
            (5, 9, 60, groupA, teacher1, math, "Linear equations", new[] { alice, bob, carol }),
            (10, 9, 60, groupA, teacher1, math, "Quadratic equations", new[] { alice, bob, carol }),
            (12, 9, 60, groupA, teacher1, math, "Word problems", new[] { alice, bob, carol }),
            (4, 11, 60, groupC, teacher1, english, "Present perfect tense", new[] { grace, henry }),
            (11, 11, 60, groupC, teacher1, english, "Reading comprehension", new[] { grace, henry }),
            (3, 14, 60, groupB, teacher2, physics, "Newton's laws", new[] { david, eva, frank }),
            (6, 14, 90, groupB, teacher2, physics, "Kinematics", new[] { david, eva, frank }),
            (13, 14, 60, groupB, teacher2, physics, "Energy conservation", new[] { david, eva, frank }),
            (7, 16, 60, groupB, teacher2, chemistry, "Periodic table", new[] { david, eva, frank }),
        };

        var rnd = new Random(42);

        foreach (var plan in lessonPlans)
        {
            var start = monthStart
                .AddDays(plan.Day - 1)
                .AddHours(plan.Hour);

            // Keep every seeded lesson in the past relative to "today" so
            // it reads as an already-conducted class (attendance/grades
            // already recorded, counted towards this month's salary).
            if (start.Date > today)
            {
                start = start.AddMonths(-1);
            }

            var lesson = new Lesson
            {
                StartTime = start,
                EndTime = start.AddMinutes(plan.DurationMin),
                Topic = plan.Topic,
                Group = plan.Group,
                Teacher = plan.Teacher,
                Subject = plan.Subject
            };

            foreach (var student in plan.Students)
            {
                var roll = rnd.Next(100);

                var status = roll switch
                {
                    < 70 => AttendanceStatus.Present,
                    < 85 => AttendanceStatus.Late,
                    < 95 => AttendanceStatus.Absent,
                    _ => AttendanceStatus.Excused
                };

                lesson.Attendances.Add(new Attendance
                {
                    Student = student,
                    Status = status
                });

                if (status != AttendanceStatus.Absent &&
                    rnd.Next(100) < 70)
                {
                    lesson.Grades.Add(new Grade
                    {
                        Student = student,
                        Subject = plan.Subject,
                        Teacher = plan.Teacher,
                        Value = rnd.Next(3, 6),
                        Date = DateTime.SpecifyKind(
                            start.Date, DateTimeKind.Utc)
                    });
                }
            }

            dbContext.Lessons.Add(lesson);
        }

        dbContext.Schedules.AddRange(
            new Schedule
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Group = groupA,
                Teacher = teacher1,
                Subject = math
            },
            new Schedule
            {
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(12, 0),
                Group = groupC,
                Teacher = teacher1,
                Subject = english
            },
            new Schedule
            {
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                Group = groupB,
                Teacher = teacher2,
                Subject = physics
            },
            new Schedule
            {
                DayOfWeek = DayOfWeek.Thursday,
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 0),
                Group = groupB,
                Teacher = teacher2,
                Subject = chemistry
            });

        dbContext.Teachers.Add(teacher2);
        dbContext.Parents.Add(parent2);
        dbContext.Subjects.AddRange(math, english, physics, chemistry);
        dbContext.Groups.AddRange(groupA, groupB, groupC);
        dbContext.Students.AddRange(
            alice, bob, carol, david, eva, frank, grace, henry);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<ApplicationUser?> GetOrCreateDemoUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user != null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, role);

        return user;
    }
}