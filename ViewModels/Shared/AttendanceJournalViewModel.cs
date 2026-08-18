using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

public class AttendanceJournalViewModel
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string? SubjectName { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public List<JournalColumn> Columns { get; set; } = new();

    public List<JournalRow> Rows { get; set; } = new();
}

public class JournalColumn
{
    public int LessonId { get; set; }

    public DateTime Date { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string? Topic { get; set; }
}

public class JournalRow
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Index-aligned to AttendanceJournalViewModel.Columns - Cells[i] is
    /// this student's cell for Columns[i], or an empty (unmarked) cell.
    /// </summary>
    public List<JournalCell> Cells { get; set; } = new();

    public double AttendanceRate { get; set; }
}

public class JournalCell
{
    public int? AttendanceId { get; set; }

    public AttendanceStatus? Status { get; set; }
}
