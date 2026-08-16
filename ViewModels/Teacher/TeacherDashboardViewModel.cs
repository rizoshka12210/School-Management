namespace SchoolManagementSystem.Web.ViewModels.Teacher;

public class TeacherDashboardViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public int GroupsCount { get; set; }

    public int StudentsCount { get; set; }

    public List<TeacherLessonSummary> TodayLessons { get; set; } = new();

    public decimal CurrentMonthWorkedHours { get; set; }

    public decimal CurrentMonthSalary { get; set; }
}

public class TeacherLessonSummary
{
    public int LessonId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Topic { get; set; }

    public bool AttendanceMarked { get; set; }
}
