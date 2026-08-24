using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

public class GroupJournalViewModel
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int WeeksRequested { get; set; } = 8;

    public int TotalStudents { get; set; }

    public int TotalLessons { get; set; }

    public int MarkedAttendanceCount { get; set; }

    public int GradedEntriesCount { get; set; }

    public double AttendanceRate { get; set; }

    public double? AverageGrade { get; set; }

    public List<GroupJournalWeekViewModel> Weeks { get; set; } = new();
}

public class GroupJournalWeekViewModel
{
    public int WeekNumber { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCurrentWeek { get; set; }

    public List<GroupJournalLessonViewModel> Lessons { get; set; } = new();

    public List<GroupJournalStudentViewModel> Students { get; set; } = new();
}

public class GroupJournalLessonViewModel
{
    public int LessonId { get; set; }

    public DateTime StartTime { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public string? Topic { get; set; }
}

public class GroupJournalStudentViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public double AttendanceRate { get; set; }

    public double? AverageGrade { get; set; }

    public List<GroupJournalCellViewModel> Cells { get; set; } = new();
}

public class GroupJournalCellViewModel
{
    public int StudentId { get; set; }

    public int LessonId { get; set; }

    public AttendanceStatus? AttendanceStatus { get; set; }

    public int? GradeValue { get; set; }
}

public class GroupJournalSaveViewModel
{
    public int GroupId { get; set; }

    [Range(1, 16)]
    public int Weeks { get; set; } = 8;

    public List<GroupJournalEntryInputModel> Entries { get; set; } = new();
}

public class GroupJournalEntryInputModel
{
    public int StudentId { get; set; }

    public int LessonId { get; set; }

    public AttendanceStatus? AttendanceStatus { get; set; }

    [Range(1, 5, ErrorMessage = "Grade must be between 1 and 5")]
    public int? GradeValue { get; set; }
}
