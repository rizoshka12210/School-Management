using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.ViewModels.Parent;

public class ParentDashboardViewModel
{
    public List<ChildSummary> Children { get; set; } = new();

    public List<CalendarEvent> UpcomingEvents { get; set; } = new();
}

public class ChildSummary
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public double AttendanceRate { get; set; }

    public double AverageGrade { get; set; }

    public List<RecentGrade> RecentGrades { get; set; } = new();
}

public class RecentGrade
{
    public string SubjectName { get; set; } = string.Empty;

    public int Value { get; set; }

    public DateTime Date { get; set; }
}
