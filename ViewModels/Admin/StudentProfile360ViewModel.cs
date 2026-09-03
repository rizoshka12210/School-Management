using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class StudentProfile360ViewModel
{
    public Student Student { get; set; } = null!;
    public double AverageGrade { get; set; }
    public double AttendanceRate { get; set; }
    public StudentRiskResult Risk { get; set; } = null!;
    public List<Grade> RecentGrades { get; set; } = new();
    public List<Attendance> RecentAttendance { get; set; } = new();
    public List<TeacherCommentViewModel> TeacherComments { get; set; } = new();
    public List<ExamGrade> ExamGradeHistory { get; set; } = new();
    public HashSet<int> CurrentExamGradeIds { get; set; } = new();
}

public class TeacherCommentViewModel
{
    public string TeacherName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
