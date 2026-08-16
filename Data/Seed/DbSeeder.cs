using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Models.Entities;
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
            Roles.Parent
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
    }
}