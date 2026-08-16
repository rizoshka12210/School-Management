using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.ViewModels.Teacher;

public class AttendanceMarkViewModel
{
    public int LessonId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime LessonDate { get; set; }

    public List<AttendanceRow> Students { get; set; } = new();
}

public class AttendanceRow
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
