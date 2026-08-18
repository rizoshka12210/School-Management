using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.ViewModels.Admin;

using ParentEntity =
    SchoolManagementSystem.Web.Models.Entities.Parent;

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

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
                .ThenInclude(s => s.Group)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(p =>
                p.ApplicationUser.FullName.ToLower().Contains(value) ||
                (p.ApplicationUser.Email != null &&
                 p.ApplicationUser.Email.ToLower().Contains(value)));
        }

        ViewBag.Search = search;

        var parents = await query
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

        var existingUser =
            await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();

            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.Email.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(
            user,
            model.Password!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await LoadStudentsAsync();

            return View(model);
        }

        await _userManager.AddToRoleAsync(
            user,
            Roles.Parent);

        var parent = new ParentEntity
        {
            ApplicationUserId = user.Id
        };

        if (model.StudentIds != null &&
            model.StudentIds.Count > 0)
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

        _context.Parents.Add(parent);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Parent created successfully.";

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
            FullName = parent.ApplicationUser.FullName,
            Email = parent.ApplicationUser.Email ?? string.Empty,
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
        ParentFormViewModel model)
    {
        var parent = await _context.Parents
            .Include(p => p.ApplicationUser)
            .Include(p => p.Students)
            .FirstOrDefaultAsync(
                p => p.Id == model.Id);

        if (parent == null)
        {
            return NotFound();
        }

        var existingUser =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (existingUser != null &&
            existingUser.Id != parent.ApplicationUserId)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();

            return View(model);
        }

        var user = parent.ApplicationUser;

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

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
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await LoadStudentsAsync();

                return View(model);
            }
        }

        parent.Students.Clear();

        if (model.StudentIds != null &&
            model.StudentIds.Count > 0)
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

        TempData["Success"] =
            "Parent updated successfully.";

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
    public async Task<IActionResult> DeleteConfirmed(
        int id)
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

        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

        TempData["Success"] =
            "Parent deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStudentsAsync()
    {
        ViewBag.Students = await _context.Students
            .Include(s => s.Group)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();
    }
}