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

    public int? ComparisonSubjectId { get; set; }

    public string? ComparisonSubjectName { get; set; }

    public GroupComparisonChartViewModel? GroupComparison { get; set; }
}

public class SubjectProgressViewModel
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public int GradesCount { get; set; }

    public double AverageGrade { get; set; }
}

/// <summary>
/// One line per student in the group (the parent's child highlighted),
/// all aligned to the same month buckets - not a single blended group
/// average line.
/// </summary>
public class GroupComparisonChartViewModel
{
    public List<string> MonthLabels { get; set; } = new();

    public List<StudentSeriesViewModel> Series { get; set; } = new();
}

public class StudentSeriesViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public bool IsChild { get; set; }

    /// <summary>Aligned by index with GroupComparisonChartViewModel.MonthLabels.</summary>
    public List<double?> Values { get; set; } = new();
}
