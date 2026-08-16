using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ParentEntity = SchoolManagementSystem.Web.Models.Entities.Parent;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ParentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParentsController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    public async Task<IActionResult> Index()
    {
        var parents = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .OrderBy(p => p.ApplicationUser.FullName)
            .ToListAsync();

        return View(parents);
    }


    public async Task<IActionResult> Details(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
                .ThenInclude(s => s.Group)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        return View(parent);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadStudentsAsync();

        return View(new ParentFormViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ParentFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();
            return View(model);
        }

        var email = model.Email.Trim();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered.");

            await LoadStudentsAsync();

            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(
            user,
            model.Password!);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);

            await LoadStudentsAsync();

            return View(model);
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                Roles.Parent);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            AddIdentityErrors(roleResult);

            await LoadStudentsAsync();

            return View(model);
        }

        var parent = new ParentEntity
        {
            ApplicationUserId = user.Id
        };

        if (model.StudentIds.Count > 0)
        {
            var students = await _context.Students
                .Where(s =>
                    model.StudentIds.Contains(s.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                parent.Students.Add(student);
            }
        }

        try
        {
            _context.Parents.Add(parent);

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
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        var model = new ParentFormViewModel
        {
            Id = parent.Id,

            FullName =
                parent.ApplicationUser.FullName,

            Email =
                parent.ApplicationUser.Email
                ?? string.Empty,

            StudentIds = parent.Students
                .Select(s => s.Id)
                .ToList()
        };

        await LoadStudentsAsync();

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ParentFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();

            return View(model);
        }

        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        var user = parent.ApplicationUser;

        var email = model.Email.Trim();

        var anotherUser =
            await _userManager.FindByEmailAsync(email);

        if (anotherUser != null &&
            anotherUser.Id != user.Id)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered.");

            await LoadStudentsAsync();

            return View(model);
        }

        user.FullName = model.FullName.Trim();

        if (user.Email != email)
        {
            var emailResult =
                await _userManager.SetEmailAsync(
                    user,
                    email);

            if (!emailResult.Succeeded)
            {
                AddIdentityErrors(emailResult);

                await LoadStudentsAsync();

                return View(model);
            }

            var usernameResult =
                await _userManager.SetUserNameAsync(
                    user,
                    email);

            if (!usernameResult.Succeeded)
            {
                AddIdentityErrors(usernameResult);

                await LoadStudentsAsync();

                return View(model);
            }
        }

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);

            await LoadStudentsAsync();

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

                await LoadStudentsAsync();

                return View(model);
            }
        }

        parent.Students.Clear();

        if (model.StudentIds.Count > 0)
        {
            var students = await _context.Students
                .Where(s =>
                    model.StudentIds.Contains(s.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                parent.Students.Add(student);
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        return View(parent);
    }


    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parent == null)
        {
            return NotFound();
        }

        var user = parent.ApplicationUser;

        parent.Students.Clear();

        _context.Parents.Remove(parent);

        await _context.SaveChangesAsync();

        var result =
            await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["Error"] =
                "Parent profile was deleted, but the login account could not be deleted.";
        }

        return RedirectToAction(nameof(Index));
    }


    private async Task LoadStudentsAsync()
    {
        ViewBag.Students = await _context.Students
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),

                Text =
                    s.FirstName + " " +
                    s.LastName
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