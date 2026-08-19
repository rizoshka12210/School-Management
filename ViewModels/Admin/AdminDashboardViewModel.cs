using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int StudentsCount { get; set; }
    public int TeachersCount { get; set; }
    public int GroupsCount { get; set; }
    public int SubjectsCount { get; set; }
    public int TodayLessonsCount { get; set; }
    public double AverageGrade { get; set; }
    public double AttendanceRate { get; set; }
    public int StudentsAbsentToday { get; set; }
    public string TrendPeriod { get; set; } = "week";

    public List<Lesson> TodayLessons { get; set; } = new();
    public List<Student> RecentStudents { get; set; } = new();
    public List<StudentPerformanceViewModel> TopStudents { get; set; } = new();
    public List<StudentPerformanceViewModel> StudentsNeedingAttention { get; set; } = new();
    public List<AttendanceTrendPointViewModel> AttendanceTrend { get; set; } = new();
    public List<CalendarEvent> UpcomingEvents { get; set; } = new();
}

public class StudentPerformanceViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public double AverageGrade { get; set; }
    public double AttendanceRate { get; set; }
    public StudentRiskStatus RiskStatus { get; set; }
    public string RiskLabel { get; set; } = string.Empty;
    public string RiskCssClass { get; set; } = string.Empty;
    public string RiskIcon { get; set; } = string.Empty;
    public List<string> RiskReasons { get; set; } = new();
}

public class AttendanceTrendPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public double Rate { get; set; }
    public int Total { get; set; }
    public int Absent { get; set; }
}
