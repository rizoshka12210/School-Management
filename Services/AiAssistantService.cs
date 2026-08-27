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

    public async Task<(string Reply, string? NavigateUrl)> AskAsync(
        ClaimsPrincipal user,
        string message,
        List<ChatHistoryItem> history)
    {
        var role = user.IsInRole(Roles.Admin) ? "Admin"
            : user.IsInRole(Roles.Teacher) ? "Teacher"
            : user.IsInRole(Roles.Parent) ? "Parent"
            : string.Empty;

        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME")
        {
            return ("Ассистент пока не настроен: не указан ключ Gemini API.", null);
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
            var geminiRole = item.Role == "assistant" ? "model" : "user";

            contents.Add(new
            {
                role = geminiRole,
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
                return ("Не удалось получить ответ от ассистента. Попробуйте ещё раз чуть позже.", null);
            }

            using var doc = JsonDocument.Parse(raw);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return ("Не удалось получить ответ от ассистента. Попробуйте переформулировать вопрос.", null);
            }

            return ExtractNavigation(role, text);
        }
        catch
        {
            return ("Не удалось связаться с ассистентом. Проверьте подключение и попробуйте снова.", null);
        }
    }

    private async Task<string> BuildSystemPromptAsync(ClaimsPrincipal user)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Ты - ИИ-ассистент школьного портала School Management. " +
            "Помогай пользователю пользоваться системой и отвечай на вопросы " +
            "о данных, перечисленных ниже. ВАЖНО: язык ответа определяй только по " +
            "последнему сообщению пользователя. Поддерживаются русский, английский " +
            "и таджикский языки. Таджикский может быть написан кириллицей или " +
            "латиницей/транслитом. Если вопрос задан на английском - отвечай только " +
            "на английском; на русском - только на русском; на таджикском - только " +
            "на таджикском. Никогда не выбирай русский язык только потому, что " +
            "данные портала или инструкции ниже написаны по-русски. Отвечай кратко " +
            "и по делу. Если данных для ответа не хватает - так и скажи, не выдумывай цифры.");

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

        sb.AppendLine();
        sb.AppendLine(await BuildUpcomingEventsContextAsync());

        return sb.ToString();
    }

    private async Task<string> BuildUpcomingEventsContextAsync()
    {
        var today = DateTime.UtcNow.Date;

        var events = await _context.CalendarEvents
            .Where(e => e.Date >= today)
            .OrderBy(e => e.Date)
            .Take(15)
            .ToListAsync();

        if (!events.Any())
        {
            return "Ближайшие события школьного календаря: пока не запланировано.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Ближайшие события школьного календаря (видны всем ролям):");

        foreach (var evt in events)
        {
            var line = $"  - {evt.Date:dd.MM.yyyy}: {evt.Title}";

            if (!string.IsNullOrWhiteSpace(evt.Description))
            {
                line += $" ({evt.Description})";
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exact in-app routes the assistant is allowed to send the user to,
    /// keyed by the same section names shown in the sidebar. Kept as a
    /// whitelist so a NAVIGATE directive from the model can only ever
    /// point at a real, role-appropriate page - never an arbitrary or
    /// hallucinated URL.
    /// </summary>
    private static Dictionary<string, string> GetRoutes(string role)
    {
        return role switch
        {
            "Admin" => new Dictionary<string, string>
            {
                ["Панель управления"] = "/Admin",
                ["Ученики"] = "/Admin/Students",
                ["Родители"] = "/Admin/Parents",
                ["Учителя"] = "/Admin/Teachers",
                ["Группы"] = "/Admin/Groups",
                ["Предметы"] = "/Admin/Subjects",
                ["Уроки"] = "/Admin/Lessons",
                ["Расписание"] = "/Admin/Schedule",
                ["Посещаемость"] = "/Admin/Attendance",
                ["Оценки"] = "/Admin/Grades",
                ["Зарплата"] = "/Admin/Salary",
                ["Рейтинг"] = "/Admin/Leaderboard",
                ["Школьный календарь"] = "/Admin/Calendar",
                ["Журнал действий"] = "/Admin/ActivityLog"
            },
            "Teacher" => new Dictionary<string, string>
            {
                ["Панель управления"] = "/Teacher",
                ["Мои группы"] = "/Teacher/Groups",
                ["Ученики"] = "/Teacher/Students",
                ["Уроки"] = "/Teacher/Lessons",
                ["Посещаемость"] = "/Teacher/Attendance",
                ["Оценки"] = "/Teacher/Grades",
                ["Темы уроков"] = "/Teacher/Topics",
                ["Расписание"] = "/Teacher/Schedule",
                ["Моя зарплата"] = "/Teacher/Salary",
                ["Рейтинг"] = "/Teacher/Leaderboard",
                ["Школьный календарь"] = "/Teacher/Calendar"
            },
            "Parent" => new Dictionary<string, string>
            {
                ["Панель управления"] = "/Parent",
                ["Мой ребёнок"] = "/Parent/Child/Details",
                ["Расписание"] = "/Parent/Schedule",
                ["Оценки"] = "/Parent/Grades",
                ["Посещаемость"] = "/Parent/Attendance",
                ["Предметы"] = "/Parent/Subjects",
                ["Темы уроков"] = "/Parent/Topics",
                ["Успеваемость"] = "/Parent/Progress",
                ["Рейтинг"] = "/Parent/Leaderboard",
                ["Школьный календарь"] = "/Parent/Calendar"
            },
            _ => new Dictionary<string, string>()
        };
    }

    private static string NavHelpFor(string role)
    {
        var routes = GetRoutes(role);

        if (routes.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Доступные разделы и точные ссылки на них:");

        foreach (var (name, url) in routes)
        {
            sb.AppendLine($"  - {name}: {url}");
        }

        sb.AppendLine(
            "Если пользователь просит открыть, показать или перейти в какой-то " +
            "раздел (например 'покажи оценки', 'открой посещаемость', 'перейди в " +
            "календарь') - в конце своего ответа добавь ОТДЕЛЬНОЙ строкой ровно " +
            "'NAVIGATE:' и точную ссылку из списка выше, например 'NAVIGATE:/Admin/Grades'. " +
            "Используй ТОЛЬКО ссылки из списка выше, никогда не придумывай свои. " +
            "Если пользователь не просит открыть раздел (просто задаёт вопрос) - " +
            "не добавляй эту строку вообще.");

        return sb.ToString();
    }

    /// <summary>
    /// Pulls a trailing "NAVIGATE:&lt;url&gt;" directive out of the model's
    /// raw reply and strips it from the text shown to the user. The URL is
    /// only honored if it exactly matches one of this role's whitelisted
    /// routes, so the model can never redirect the user somewhere outside
    /// the app or to a page their role can't see.
    /// </summary>
    private static (string Text, string? NavigateUrl) ExtractNavigation(string role, string rawText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            rawText,
            @"\n?NAVIGATE:\s*(\S+)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return (rawText, null);
        }

        var candidate = match.Groups[1].Value.TrimEnd('.', ',', ')');
        var text = rawText.Substring(0, match.Index).TrimEnd();

        var isAllowed = GetRoutes(role).Values
            .Any(url => string.Equals(url, candidate, StringComparison.OrdinalIgnoreCase));

        return (text, isAllowed ? candidate : null);
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

        var sb = new StringBuilder();

        sb.AppendLine("Данные по школе:");
        sb.AppendLine($"- Учеников: {studentsCount}");
        sb.AppendLine($"- Учителей: {teachersCount}");
        sb.AppendLine($"- Групп: {groupsCount}");
        sb.AppendLine($"- Предметов: {subjectsCount}");
        sb.AppendLine($"- Уроков сегодня: {todayLessonsCount}");
        sb.AppendLine($"- Средний балл по школе: {averageGrade}");
        sb.AppendLine($"- Отсутствующих сегодня учеников: {absentToday}");

        sb.AppendLine();
        sb.AppendLine(await BuildAllStudentsListAsync());

        var teachers = await _context.Teachers
            .Include(t => t.ApplicationUser)
            .Include(t => t.Groups)
            .Include(t => t.Subjects)
            .Take(100)
            .ToListAsync();

        if (teachers.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Список учителей:");

            foreach (var teacher in teachers)
            {
                var groups = teacher.Groups.Any()
                    ? string.Join(", ", teacher.Groups.Select(g => g.Name))
                    : "нет групп";

                var subjects = teacher.Subjects.Any()
                    ? string.Join(", ", teacher.Subjects.Select(s => s.Name))
                    : "нет предметов";

                sb.AppendLine(
                    $"  - {teacher.ApplicationUser.FullName}: группы [{groups}], " +
                    $"предметы [{subjects}]");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Full student roster with grades/attendance, shared by Admin and
    /// Teacher context - both roles are meant to know about every student
    /// in the school, not just their own group (unlike the rest of the
    /// app's UI, which scopes teachers to their own groups).
    /// </summary>
    private async Task<string> BuildAllStudentsListAsync()
    {
        var students = await _context.Students
            .Include(s => s.Group)
            .Include(s => s.Parents)
                .ThenInclude(p => p.ApplicationUser)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .Take(200)
            .ToListAsync();

        if (!students.Any())
        {
            return "Список всех учеников: пока нет учеников в системе.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Список всех учеников (успеваемость и посещаемость по всем предметам):");

        foreach (var student in students)
        {
            var studentGrades = await _context.Grades
                .Where(g => g.StudentId == student.Id)
                .Select(g => g.Value)
                .ToListAsync();

            var studentAttendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .Select(a => a.Status)
                .ToListAsync();

            var avgGrade = studentGrades.Any()
                ? Math.Round(studentGrades.Average(), 2).ToString()
                : "нет оценок";

            var attRate = studentAttendances.Any()
                ? Math.Round(
                    studentAttendances.Count(s => s == AttendanceStatus.Present) * 100.0
                        / studentAttendances.Count,
                    1) + "%"
                : "нет данных";

            var parents = student.Parents.Any()
                ? string.Join(", ", student.Parents.Select(p => p.ApplicationUser.FullName))
                : "не назначены";

            sb.AppendLine(
                $"  - {student.FirstName} {student.LastName} " +
                $"(группа {student.Group?.Name ?? "-"}): " +
                $"средний балл {avgGrade}, посещаемость {attRate}, родители: {parents}");
        }

        return sb.ToString();
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

        sb.AppendLine();
        sb.AppendLine(await BuildAllStudentsListAsync());

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
