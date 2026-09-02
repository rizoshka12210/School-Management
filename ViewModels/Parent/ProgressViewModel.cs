namespace SchoolManagementSystem.Web.ViewModels.Parent;

public class ProgressViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string? GroupName { get; set; }

    public double AverageGrade { get; set; }

    public int GradesCount { get; set; }

    public int TotalLessons { get; set; }

    public int PresentCount { get; set; }

    public int LateCount { get; set; }

    public int AbsentCount { get; set; }

    public int ExcusedCount { get; set; }

    public double AttendanceRate { get; set; }

    public List<SubjectProgressViewModel> Subjects { get; set; } = new();
}

public class SubjectProgressViewModel
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public int GradesCount { get; set; }

    public double AverageGrade { get; set; }
}
