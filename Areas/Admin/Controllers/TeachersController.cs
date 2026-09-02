using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SchoolManagementSystem.Web;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Identity;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Admin;

using TeacherEntity =
    SchoolManagementSystem.Web.Models.Entities.Teacher;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminAndDirector)]
public class TeachersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ActivityLogService _activityLog;

    public TeachersController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ActivityLogService activityLog)
    {
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _activityLog = activityLog;
    }


    // ==========================================
    // INDEX
    // ==========================================

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();

            query = query.Where(t =>
                t.ApplicationUser.FullName.ToLower().Contains(value) ||
                t.ApplicationUser.Email!.ToLower().Contains(value));
        }

        ViewBag.Search = search;

        var teachers = await query
            .OrderBy(t => t.ApplicationUser.FullName)
            .ToListAsync();

        return View(teachers);
    }


    // ==========================================
    // DETAILS
    // ==========================================

    public async Task<IActionResult> Details(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }

        ViewBag.Notices = await _context.TeacherNotices
            .Where(n => n.TeacherId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return View(teacher);
    }


    // ==========================================
    // NOTIFY (SEND MESSAGE) GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Notify(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }

        return View(new TeacherNoticeFormViewModel
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.ApplicationUser.FullName
        });
    }


    // ==========================================
    // NOTIFY (SEND MESSAGE) POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Notify(TeacherNoticeFormViewModel model)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == model.TeacherId);

        if (teacher == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.TeacherName = teacher.ApplicationUser.FullName;
            return View(model);
        }

        var notice = new Models.Entities.TeacherNotice
        {
            TeacherId = teacher.Id,
            Message = model.Message.Trim()
        };

        _context.TeacherNotices.Add(notice);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            $"Admin sent a message to teacher \"{teacher.ApplicationUser.FullName}\"");

        TempData["Success"] =
            _localizer["Message sent to teacher."].Value;

        return RedirectToAction(nameof(Details), new { id = teacher.Id });
    }


    // ==========================================
    // EDIT NOTICE GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditNotice(int id)
    {
        var notice = await _context.TeacherNotices
            .Include(n => n.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notice == null)
        {
            return NotFound();
        }

        return View(new TeacherNoticeFormViewModel
        {
            Id = notice.Id,
            TeacherId = notice.TeacherId,
            TeacherName = notice.Teacher.ApplicationUser.FullName,
            Message = notice.Message
        });
    }


    // ==========================================
    // EDIT NOTICE POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditNotice(TeacherNoticeFormViewModel model)
    {
        var notice = await _context.TeacherNotices
            .Include(n => n.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(n => n.Id == model.Id);

        if (notice == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.TeacherName = notice.Teacher.ApplicationUser.FullName;
            return View(model);
        }

        notice.Message = model.Message.Trim();

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            $"Admin edited a message sent to teacher \"{notice.Teacher.ApplicationUser.FullName}\"");

        TempData["Success"] =
            _localizer["Message updated."].Value;

        return RedirectToAction(nameof(Details), new { id = notice.TeacherId });
    }


    // ==========================================
    // DELETE NOTICE GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteNotice(int id)
    {
        var notice = await _context.TeacherNotices
            .Include(n => n.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notice == null)
        {
            return NotFound();
        }

        return View(notice);
    }


    // ==========================================
    // DELETE NOTICE POST
    // ==========================================

    [HttpPost]
    [ActionName("DeleteNotice")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteNoticeConfirmed(int id)
    {
        var notice = await _context.TeacherNotices
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notice == null)
        {
            return NotFound();
        }

        var teacherId = notice.TeacherId;

        _context.TeacherNotices.Remove(notice);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            _localizer["Message deleted."].Value;

        return RedirectToAction(nameof(Details), new { id = teacherId });
    }


    // ==========================================
    // CREATE GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();

        return View(new TeacherFormViewModel());
    }


    // ==========================================
    // CREATE POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(
        TeacherFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();

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

            await LoadFormDataAsync();

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


        if (model.GroupIds != null &&
            model.GroupIds.Count > 0)
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


        if (model.SubjectIds != null &&
            model.SubjectIds.Count > 0)
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


        _context.Teachers.Add(teacher);

        await _context.SaveChangesAsync();


        TempData["Success"] =
            _localizer["Teacher created successfully."].Value;

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // EDIT GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }


        var model = new TeacherFormViewModel
        {
            Id = teacher.Id,

            FullName =
                teacher.ApplicationUser.FullName,

            Email =
                teacher.ApplicationUser.Email ?? string.Empty,

            HourlyRate =
                teacher.HourlyRate,

            GroupIds =
                teacher.Groups
                    .Select(g => g.Id)
                    .ToList(),

            SubjectIds =
                teacher.Subjects
                    .Select(s => s.Id)
                    .ToList()
        };


        await LoadFormDataAsync();

        return View(model);
    }


    // ==========================================
    // EDIT POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(
        TeacherFormViewModel model)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(
                t => t.Id == model.Id);

        if (teacher == null)
        {
            return NotFound();
        }


        var existingUser =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (existingUser != null &&
            existingUser.Id !=
            teacher.ApplicationUserId)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }


        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();

            return View(model);
        }


        var user = teacher.ApplicationUser;

        user.FullName =
            model.FullName.Trim();

        user.Email =
            model.Email.Trim();

        user.UserName =
            model.Email.Trim();


        var userUpdateResult =
            await _userManager.UpdateAsync(user);

        if (!userUpdateResult.Succeeded)
        {
            foreach (var error in
                     userUpdateResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await LoadFormDataAsync();

            return View(model);
        }


        // PASSWORD IS OPTIONAL ON EDIT

        if (!string.IsNullOrWhiteSpace(
                model.Password))
        {
            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(
                        user);

            var passwordResult =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.Password);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in
                         passwordResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await LoadFormDataAsync();

                return View(model);
            }
        }


        var previousHourlyRate = teacher.HourlyRate;

        teacher.HourlyRate =
            model.HourlyRate;


        // GROUPS

        teacher.Groups.Clear();

        if (model.GroupIds != null &&
            model.GroupIds.Count > 0)
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


        // SUBJECTS

        teacher.Subjects.Clear();

        if (model.SubjectIds != null &&
            model.SubjectIds.Count > 0)
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

        if (previousHourlyRate != teacher.HourlyRate)
        {
            await _activityLog.LogAsync(
                $"Admin changed {teacher.ApplicationUser.FullName}'s salary rate");
        }

        TempData["Success"] =
            _localizer["Teacher updated successfully."].Value;

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // DELETE GET
    // ==========================================

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }

        return View(teacher);
    }


    // ==========================================
    // DELETE POST
    // ==========================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }


        var hasLessons =
            await _context.Lessons
                .AnyAsync(l =>
                    l.TeacherId == id);

        var hasSchedules =
            await _context.Schedules
                .AnyAsync(s =>
                    s.TeacherId == id);

        var hasGrades =
            await _context.Grades
                .AnyAsync(g =>
                    g.TeacherId == id);


        if (hasLessons ||
            hasSchedules ||
            hasGrades)
        {
            TempData["Error"] =
                _localizer["This teacher cannot be deleted because they have lessons, schedules or grades."].Value;

            return RedirectToAction(nameof(Index));
        }


        var user =
            teacher.ApplicationUser;


        _context.Teachers.Remove(teacher);

        await _context.SaveChangesAsync();


        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }


        TempData["Success"] =
            _localizer["Teacher deleted successfully."].Value;

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // FORM DATA
    // ==========================================

    private async Task LoadFormDataAsync()
    {
        ViewBag.Groups =
            await _context.Groups
                .OrderBy(g => g.Name)
                .ToListAsync();

        ViewBag.Subjects =
            await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();
    }
}