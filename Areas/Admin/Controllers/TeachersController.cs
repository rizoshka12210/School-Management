using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Admin;
using TeacherEntity = SchoolManagementSystem.Web.Models.Entities.Teacher;
namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class TeachersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeachersController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    public async Task<IActionResult> Index()
    {
        var teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();

        return View(teachers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
            return NotFound();

        return View(teacher);
    }

   

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();

        return View(new TeacherFormViewModel());
    }

   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TeacherFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        var existingUser =
            await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered.");

            await LoadLookupsAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            EmailConfirmed = true
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                model.Password!);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await LoadLookupsAsync();
            return View(model);
        }

        await _userManager.AddToRoleAsync(
            user,
            Roles.Teacher);

        var teacher = new TeacherEntity
        {
            ApplicationUserId = user.Id,
            HourlyRate = model.HourlyRate
        };

        if (model.GroupIds.Count > 0)
        {
            var groups = await _context.Groups
                .Where(g => model.GroupIds.Contains(g.Id))
                .ToListAsync();

            foreach (var group in groups)
            {
                teacher.Groups.Add(group);
            }
        }

        if (model.SubjectIds.Count > 0)
        {
            var subjects = await _context.Subjects
                .Where(s => model.SubjectIds.Contains(s.Id))
                .ToListAsync();

            foreach (var subject in subjects)
            {
                teacher.Subjects.Add(subject);
            }
        }

        try
        {
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
        }
        catch
        {

            await _userManager.DeleteAsync(user);
            throw;
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
            return NotFound();

        var model = new TeacherFormViewModel
        {
            Id = teacher.Id,
            FullName = teacher.ApplicationUser.FullName,
            Email = teacher.ApplicationUser.Email ?? string.Empty,
            HourlyRate = teacher.HourlyRate,

            GroupIds = teacher.Groups
                .Select(g => g.Id)
                .ToList(),

            SubjectIds = teacher.Subjects
                .Select(s => s.Id)
                .ToList()
        };

        await LoadLookupsAsync();

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        TeacherFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
            return NotFound();

        var anotherUser =
            await _userManager.FindByEmailAsync(model.Email);

        if (anotherUser != null &&
            anotherUser.Id != teacher.ApplicationUserId)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered.");

            await LoadLookupsAsync();
            return View(model);
        }

        var user = teacher.ApplicationUser;

        user.FullName = model.FullName.Trim();

        var email = model.Email.Trim();

        if (user.Email != email)
        {
            var emailResult =
                await _userManager.SetEmailAsync(
                    user,
                    email);

            if (!emailResult.Succeeded)
            {
                AddIdentityErrors(emailResult);

                await LoadLookupsAsync();
                return View(model);
            }

            var usernameResult =
                await _userManager.SetUserNameAsync(
                    user,
                    email);

            if (!usernameResult.Succeeded)
            {
                AddIdentityErrors(usernameResult);

                await LoadLookupsAsync();
                return View(model);
            }
        }

        var updateUserResult =
            await _userManager.UpdateAsync(user);

        if (!updateUserResult.Succeeded)
        {
            AddIdentityErrors(updateUserResult);

            await LoadLookupsAsync();
            return View(model);
        }


        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);

            var passwordResult =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.Password);

            if (!passwordResult.Succeeded)
            {
                AddIdentityErrors(passwordResult);

                await LoadLookupsAsync();
                return View(model);
            }
        }

        teacher.HourlyRate = model.HourlyRate;

        teacher.Groups.Clear();

        if (model.GroupIds.Count > 0)
        {
            var groups = await _context.Groups
                .Where(g =>
                    model.GroupIds.Contains(g.Id))
                .ToListAsync();

            foreach (var group in groups)
            {
                teacher.Groups.Add(group);
            }
        }

        teacher.Subjects.Clear();

        if (model.SubjectIds.Count > 0)
        {
            var subjects = await _context.Subjects
                .Where(s =>
                    model.SubjectIds.Contains(s.Id))
                .ToListAsync();

            foreach (var subject in subjects)
            {
                teacher.Subjects.Add(subject);
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }



    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
            return NotFound();

        return View(teacher);
    }



    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
            return NotFound();

        var hasLessons = await _context.Lessons
            .AnyAsync(l => l.TeacherId == id);

        var hasSchedules = await _context.Schedules
            .AnyAsync(s => s.TeacherId == id);

        var hasGrades = await _context.Grades
            .AnyAsync(g => g.TeacherId == id);

        if (hasLessons || hasSchedules || hasGrades)
        {
            TempData["Error"] =
                "This teacher cannot be deleted because they are used in lessons, schedules or grades.";

            return RedirectToAction(nameof(Index));
        }

        var user = teacher.ApplicationUser;

        teacher.Groups.Clear();
        teacher.Subjects.Clear();

        _context.Teachers.Remove(teacher);

        await _context.SaveChangesAsync();

        var deleteUserResult =
            await _userManager.DeleteAsync(user);

        if (!deleteUserResult.Succeeded)
        {
            TempData["Error"] =
                "Teacher profile was removed, but the user account could not be deleted.";
        }

        return RedirectToAction(nameof(Index));
    }


    private async Task LoadLookupsAsync()
    {
        ViewBag.Groups = await _context.Groups
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();

        ViewBag.Subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToListAsync();
    }

    private void AddIdentityErrors(
        IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }
    }
}