using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Authorization;

public class OwnershipHelper
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OwnershipHelper(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> ParentOwnsStudentAsync(
        ClaimsPrincipal user,
        int studentId)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return false;

        return await _context.Parents
            .AnyAsync(p =>
                p.ApplicationUserId == userId &&
                p.Students.Any(s => s.Id == studentId));
    }

    public async Task<bool> TeacherOwnsGroupAsync(
        ClaimsPrincipal user,
        int groupId)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return false;

        return await _context.Teachers
            .AnyAsync(t =>
                t.ApplicationUserId == userId &&
                t.Groups.Any(g => g.Id == groupId));
    }

    public async Task<bool> TeacherOwnsStudentAsync(
        ClaimsPrincipal user,
        int studentId)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return false;

        return await _context.Teachers
            .AnyAsync(t =>
                t.ApplicationUserId == userId &&
                t.Groups.Any(g =>
                    g.Students.Any(s => s.Id == studentId)));
    }

    public async Task<bool> TeacherOwnsLessonAsync(
        ClaimsPrincipal user,
        int lessonId)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return false;

        return await _context.Lessons
            .AnyAsync(l =>
                l.Id == lessonId &&
                l.Teacher.ApplicationUserId == userId);
    }

    public async Task<int?> GetCurrentTeacherIdAsync(
        ClaimsPrincipal user)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return null;

        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t =>
                t.ApplicationUserId == userId);

        return teacher?.Id;
    }

    /// <summary>
    /// True only for the single teacher (if any) the admin has
    /// designated via Admin > Big Exam > Grader Access.
    /// </summary>
    public async Task<bool> IsCurrentUserBigExamGraderAsync(
        ClaimsPrincipal user)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return false;

        return await _context.Teachers
            .AnyAsync(t =>
                t.ApplicationUserId == userId &&
                t.IsBigExamGrader);
    }

    public async Task<int?> GetCurrentParentIdAsync(
        ClaimsPrincipal user)
    {
        var userId = _userManager.GetUserId(user);

        if (userId == null)
            return null;

        var parent = await _context.Parents
            .FirstOrDefaultAsync(p =>
                p.ApplicationUserId == userId);

        return parent?.Id;
    }
}