using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.ViewModels.Assistant;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Answers questions about how to use the portal and about the signed-in
/// user's own data. Context is rebuilt from the database on every request
/// (same "compute, don't store" approach as SalaryService) so the model
/// never sees stale or someone else's data - each role's context is built
/// through the same ownership-scoped queries the rest of the app uses.
/// </summary>
public class AiAssistantService
{
    private readonly AppDbContext _context;
    private readonly OwnershipHelper _ownership;
    private readonly AchievementService _achievements;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AiAssistantService(
        AppDbContext context,
        OwnershipHelper ownership,
        AchievementService achievements,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _ownership = ownership;
        _achievements = achievements;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> AskAsync(
        ClaimsPrincipal user,
        string message,
        List<ChatHistoryItem> history)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME")
        {
            return "Ассистент пока не настроен: не указан ключ Gemini API.";
        }

        var model = _configuration["Gemini:Model"];

        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gemini-flash-lite-latest";
        }

        var systemPrompt = await BuildSystemPromptAsync(user);

        var contents = new List<object>();

        foreach (var item in history.TakeLast(12))
        {
            var role = item.Role == "assistant" ? "model" : "user";

            contents.Add(new
            {
                role,
                parts = new object[] { new { text = item.Text } }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new object[] { new { text = message } }
        });

        var requestBody = new
        {
            contents,
            systemInstruction = new
            {
                parts = new object[] { new { text = systemPrompt } }
            },
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 700
            }
        };

        var client = _httpClientFactory.CreateClient("Gemini");

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, requestBody);

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "Не удалось получить ответ от ассистента. Попробуйте ещё раз чуть позже.";
            }

            using var doc = JsonDocument.Parse(raw);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(text)
                ? "Не удалось получить ответ от ассистента. Попробуйте переформулировать вопрос."
                : text;
        }
        catch
        {
            return "Не удалось связаться с ассистентом. Проверьте подключение и попробуйте снова.";
        }
    }

    private async Task<string> BuildSystemPromptAsync(ClaimsPrincipal user)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Ты - ИИ-ассистент школьного портала School Management. " +
            "Помогай пользователю пользоваться системой и отвечай на вопросы " +
            "о данных, перечисленных ниже. Отвечай на том же языке, на котором " +
            "задан вопрос (русский или английский). Отвечай кратко и по делу. " +
            "Если данных для ответа не хватает - так и скажи, не выдумывай цифры.");

        sb.AppendLine();

        if (user.IsInRole(Roles.Admin))
        {
            sb.AppendLine(NavHelpFor("Admin"));
            sb.AppendLine();
            sb.AppendLine(await BuildAdminContextAsync());
        }
        else if (user.IsInRole(Roles.Teacher))
        {
            sb.AppendLine(NavHelpFor("Teacher"));
            sb.AppendLine();
            sb.AppendLine(await BuildTeacherContextAsync(user));
        }
        else if (user.IsInRole(Roles.Parent))
        {
            sb.AppendLine(NavHelpFor("Parent"));
            sb.AppendLine();
            sb.AppendLine(await BuildParentContextAsync(user));
        }

        return sb.ToString();
    }

    private static string NavHelpFor(string role)
    {
        return role switch
        {
            "Admin" =>
                "Разделы для администратора: Панель управления, Ученики, Родители, " +
                "Учителя, Группы, Предметы, Уроки, Расписание, Посещаемость, Оценки, " +
                "Зарплата, Рейтинг, Школьный календарь, Журнал действий. Все разделы " +
                "доступны через меню слева.",
            "Teacher" =>
                "Разделы для учителя: Панель управления, Мои группы, Ученики, Уроки, " +
                "Посещаемость (отметить и посмотреть журнал), Оценки, Темы уроков, " +
                "Расписание, Моя зарплата, Рейтинг, Школьный календарь. Все разделы " +
                "доступны через меню слева.",
            "Parent" =>
                "Разделы для родителя: Панель управления, Мой ребёнок (профиль, " +
                "достижения, комментарии учителя), Расписание, Оценки, Посещаемость, " +
                "Предметы, Темы уроков, Успеваемость, Рейтинг, Школьный календарь. " +
                "Все разделы доступны через меню слева.",
            _ => string.Empty
        };
    }

    private async Task<string> BuildAdminContextAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var studentsCount = await _context.Students.CountAsync();
        var teachersCount = await _context.Teachers.CountAsync();
        var groupsCount = await _context.Groups.CountAsync();
        var subjectsCount = await _context.Subjects.CountAsync();

        var todayLessonsCount = await _context.Lessons
            .CountAsync(l => l.StartTime >= today && l.StartTime < tomorrow);

        var allGrades = await _context.Grades.Select(g => g.Value).ToListAsync();

        var averageGrade = allGrades.Any()
            ? Math.Round(allGrades.Average(), 2)
            : 0;

        var absentToday = await _context.Attendances
            .Where(a =>
                a.Lesson.StartTime >= today &&
                a.Lesson.StartTime < tomorrow &&
                a.Status == AttendanceStatus.Absent)
            .Select(a => a.StudentId)
            .Distinct()
            .CountAsync();

        return
            "Данные по школе:\n" +
            $"- Учеников: {studentsCount}\n" +
            $"- Учителей: {teachersCount}\n" +
            $"- Групп: {groupsCount}\n" +
            $"- Предметов: {subjectsCount}\n" +
            $"- Уроков сегодня: {todayLessonsCount}\n" +
            $"- Средний балл по школе: {averageGrade}\n" +
            $"- Отсутствующих сегодня учеников: {absentToday}";
    }

    private async Task<string> BuildTeacherContextAsync(ClaimsPrincipal user)
    {
        var teacherId = await _ownership.GetCurrentTeacherIdAsync(user);

        if (teacherId == null)
        {
            return "Данные учителя не найдены.";
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayLessons = await _context.Lessons
            .Where(l =>
                l.TeacherId == teacherId &&
                l.StartTime >= today &&
                l.StartTime < tomorrow)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .Include(l => l.Attendances)
            .Include(l => l.Grades)
            .OrderBy(l => l.StartTime)
            .ToListAsync();

        var groupsCount = await _context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .CountAsync();

        var studentsCount = await _context.Teachers
            .Where(t => t.Id == teacherId)
            .SelectMany(t => t.Groups)
            .SelectMany(g => g.Students)
            .Select(s => s.Id)
            .Distinct()
            .CountAsync();

        var missingGrades = todayLessons.Sum(l =>
            l.Group.Students.Count(s => !l.Grades.Any(g => g.StudentId == s.Id)));

        var attendanceNotCompleted = todayLessons.Count(l => !l.Attendances.Any());

        var sb = new StringBuilder();

        sb.AppendLine("Данные учителя:");
        sb.AppendLine($"- Групп: {groupsCount}");
        sb.AppendLine($"- Учеников: {studentsCount}");
        sb.AppendLine($"- Уроков сегодня: {todayLessons.Count}");
        sb.AppendLine($"- Не выставлено оценок сегодня: {missingGrades}");
        sb.AppendLine($"- Посещаемость не отмечена (уроков): {attendanceNotCompleted}");

        if (todayLessons.Any())
        {
            sb.AppendLine("Уроки сегодня:");

            foreach (var lesson in todayLessons)
            {
                sb.AppendLine(
                    $"  - {lesson.StartTime:HH:mm} {lesson.Subject.Name}, " +
                    $"группа {lesson.Group.Name}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildParentContextAsync(ClaimsPrincipal user)
    {
        var parentId = await _ownership.GetCurrentParentIdAsync(user);

        if (parentId == null)
        {
            return "Данные родителя не найдены.";
        }

        var students = await _context.Parents
            .Where(p => p.Id == parentId)
            .SelectMany(p => p.Students)
            .Include(s => s.Group)
            .ToListAsync();

        if (!students.Any())
        {
            return "К аккаунту родителя не привязано ни одного ребёнка.";
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var sevenDaysAgo = today.AddDays(-7);

        var sb = new StringBuilder();
        sb.AppendLine("Данные о детях:");

        foreach (var student in students)
        {
            var attendances = await _context.Attendances
                .Include(a => a.Lesson)
                    .ThenInclude(l => l.Subject)
                .Where(a => a.StudentId == student.Id)
                .ToListAsync();

            var total = attendances.Count;

            var present = attendances.Count(a => a.Status == AttendanceStatus.Present);

            var attendanceRate = total == 0
                ? 0
                : Math.Round(present * 100.0 / total, 1);

            var grades = await _context.Grades
                .Where(g => g.StudentId == student.Id)
                .Include(g => g.Subject)
                .OrderByDescending(g => g.Date)
                .ToListAsync();

            var averageGrade = grades.Any()
                ? Math.Round(grades.Average(g => g.Value), 2)
                : 0;

            var lessonsToday = student.GroupId == null
                ? 0
                : await _context.Lessons
                    .CountAsync(l =>
                        l.GroupId == student.GroupId &&
                        l.StartTime >= today &&
                        l.StartTime < tomorrow);

            sb.AppendLine(
                $"- {student.FirstName} {student.LastName} " +
                $"(группа {student.Group?.Name ?? "не назначена"}): " +
                $"средний балл {averageGrade}, посещаемость {attendanceRate}%, " +
                $"уроков сегодня {lessonsToday}");

            if (grades.Any())
            {
                var recent = grades.Take(3)
                    .Select(g => $"{g.Subject.Name} - {g.Value} ({g.Date:dd.MM})");

                sb.AppendLine($"  Последние оценки: {string.Join(", ", recent)}");
            }

            var recentMissed = attendances
                .Where(a =>
                    a.Status == AttendanceStatus.Absent &&
                    a.Lesson.StartTime >= sevenDaysAgo)
                .OrderByDescending(a => a.Lesson.StartTime)
                .FirstOrDefault();

            if (recentMissed != null)
            {
                sb.AppendLine(
                    $"  Недавний пропуск: {recentMissed.Lesson.Subject.Name} " +
                    $"({recentMissed.Lesson.StartTime:dd.MM})");
            }

            var badges = await _achievements.GetBadgesAsync(student.Id);
            var earned = badges.Where(b => b.Earned).Select(b => b.NameKey).ToList();

            sb.AppendLine(
                earned.Any()
                    ? $"  Достижения: {string.Join(", ", earned)}"
                    : "  Достижения: пока нет заработанных");
        }

        return sb.ToString();
    }
}
