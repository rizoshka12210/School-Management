using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.ViewModels.Notifications;

namespace SchoolManagementSystem.Web.Services;

public class NotificationService
{
    public const string SeenCookieName = "school-notifications-seen";

    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationItemViewModel>> GetForUserAsync(
        ClaimsPrincipal user,
        int take = 12)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return new();
        }

        if (user.IsInRole(Roles.Admin))
        {
            return await GetAdminNotificationsAsync(take);
        }

        if (user.IsInRole(Roles.Teacher))
        {
            return await GetTeacherNotificationsAsync(user, take);
        }

        if (user.IsInRole(Roles.Parent))
        {
            return await GetParentNotificationsAsync(user, take);
        }

        return new();
    }

    public async Task<int> GetUnreadCountAsync(
        ClaimsPrincipal user,
        string? seenFingerprint)
    {
        var items = await GetForUserAsync(user, 20);

        if (items.Count == 0)
        {
            return 0;
        }

        var currentFingerprint = BuildFingerprint(user, items);

        return string.Equals(
            currentFingerprint,
            seenFingerprint,
            StringComparison.Ordinal)
            ? 0
            : items.Count;
    }

    public async Task<string?> GetCurrentFingerprintAsync(
        ClaimsPrincipal user)
    {
        var items = await GetForUserAsync(user, 20);

        return items.Count == 0
            ? null
            : BuildFingerprint(user, items);
    }

    private static string BuildFingerprint(
        ClaimsPrincipal user,
        IEnumerable<NotificationItemViewModel> items)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.Identity?.Name
            ?? "anonymous";

        var payload = userId + "|" + string.Join(
            "||",
            items.Select(item =>
                $"{item.Title}|{item.Message}|{item.Url}|{item.Kind}|{item.OccurredAt:O}"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private async Task<List<NotificationItemViewModel>> GetAdminNotificationsAsync(int take)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var items = new List<NotificationItemViewModel>();

        var newestStudents = await _context.Students
            .Include(s => s.Group)
            .OrderByDescending(s => s.Id)
            .Take(4)
            .ToListAsync();

        items.AddRange(newestStudents.Select(s => new NotificationItemViewModel
        {
            Icon = "👤",
            Title = "Student joined",
            Message = $"{s.FirstName} {s.LastName} joined {(s.Group?.Name ?? "school")}",
            Url = $"/Admin/Students/Details/{s.Id}",
            Kind = "success"
        }));

        var lessonsToday = await _context.Lessons
            .CountAsync(l => l.StartTime >= today && l.StartTime < tomorrow);

        items.Insert(0, new NotificationItemViewModel
        {
            Icon = "📚",
            Title = "Today's lessons",
            Message = $"{lessonsToday} lesson(s) scheduled today",
            Url = "/Admin/Lessons",
            Kind = "info"
        });

        var absentToday = await _context.Attendances
            .Where(a =>
                a.Lesson.StartTime >= today &&
                a.Lesson.StartTime < tomorrow &&
                a.Status == AttendanceStatus.Absent)
            .Select(a => a.StudentId)
            .Distinct()
            .CountAsync();

        if (absentToday > 0)
        {
            items.Insert(0, new NotificationItemViewModel
            {
                Icon = "⚠️",
                Title = "Absence alert",
                Message = $"{absentToday} student(s) are absent today",
                Url = "/Admin/Attendance",
                Kind = "warning"
            });
        }

        return items.Take(take).ToList();
    }

    private async Task<List<NotificationItemViewModel>> GetTeacherNotificationsAsync(
        ClaimsPrincipal user,
        int take)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new();
        }

        var teacherId = await _context.Teachers
            .Where(t => t.ApplicationUserId == userId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (!teacherId.HasValue)
        {
            return new();
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var lessons = await _context.Lessons
            .Where(l => l.TeacherId == teacherId.Value &&
                        l.StartTime >= today &&
                        l.StartTime < tomorrow)
            .Include(l => l.Group)
                .ThenInclude(g => g.Students)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .OrderBy(l => l.StartTime)
            .ToListAsync();

        var items = lessons.Select(l => new NotificationItemViewModel
        {
            Icon = l.Attendances.Count >= l.Group.Students.Count && l.Group.Students.Any()
                ? "✅"
                : "📝",
            Title = l.Attendances.Count >= l.Group.Students.Count && l.Group.Students.Any()
                ? "Attendance completed"
                : "Attendance pending",
            Message = $"{l.Subject.Name} · {l.Group.Name} · {l.StartTime:HH:mm}",
            Url = $"/Teacher/Attendance/Mark?lessonId={l.Id}",
            OccurredAt = l.StartTime,
            Kind = l.Attendances.Count >= l.Group.Students.Count && l.Group.Students.Any()
                ? "success"
                : "warning"
        }).ToList();

        return items.Take(take).ToList();
    }

    private async Task<List<NotificationItemViewModel>> GetParentNotificationsAsync(
        ClaimsPrincipal user,
        int take)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new();
        }

        var parent = await _context.Parents
            .Include(p => p.Students)
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (parent == null || !parent.Students.Any())
        {
            return new();
        }

        var studentIds = parent.Students.Select(s => s.Id).ToList();
        var items = new List<NotificationItemViewModel>();

        var grades = await _context.Grades
            .Where(g => studentIds.Contains(g.StudentId))
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .OrderByDescending(g => g.Date)
            .Take(6)
            .ToListAsync();

        items.AddRange(grades.Select(g => new NotificationItemViewModel
        {
            Icon = "⭐",
            Title = "New grade",
            Message = $"{g.Student.FirstName}: {g.Subject.Name} — {g.Value}",
            Url = $"/Parent/Grades?studentId={g.StudentId}",
            OccurredAt = g.Date,
            Kind = g.Value >= 4 ? "success" : "info"
        }));

        var absences = await _context.Attendances
            .Where(a => studentIds.Contains(a.StudentId) &&
                        a.Status == AttendanceStatus.Absent)
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .OrderByDescending(a => a.Lesson.StartTime)
            .Take(6)
            .ToListAsync();

        items.AddRange(absences.Select(a => new NotificationItemViewModel
        {
            Icon = "⚠️",
            Title = "Missed lesson",
            Message = $"{a.Student.FirstName} missed {a.Lesson.Subject.Name}",
            Url = $"/Parent/Attendance?studentId={a.StudentId}",
            OccurredAt = a.Lesson.StartTime,
            Kind = "danger"
        }));

        return items
            .OrderByDescending(i => i.OccurredAt ?? DateTime.UtcNow)
            .Take(take)
            .ToList();
    }
}
