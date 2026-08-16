using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.Models.Entities;

public class Attendance
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}