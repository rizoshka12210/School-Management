using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.Services;

public class StudentRiskService
{
    public StudentRiskResult Evaluate(Student student)
    {
        var averageGrade = student.Grades.Any()
            ? student.Grades.Average(g => g.Value)
            : 0;

        var attendanceTotal = student.Attendances.Count;
        var attended = student.Attendances.Count(a =>
            a.Status != AttendanceStatus.Absent);

        var attendanceRate = attendanceTotal == 0
            ? 0
            : attended * 100.0 / attendanceTotal;

        var reasons = new List<string>();

        if (attendanceTotal == 0)
        {
            reasons.Add("No attendance data yet");
        }
        else if (attendanceRate < 70)
        {
            reasons.Add("Attendance below 70%");
        }
        else if (attendanceRate < 85)
        {
            reasons.Add("Attendance below 85%");
        }

        if (!student.Grades.Any())
        {
            reasons.Add("No grades recorded yet");
        }
        else if (averageGrade < 60)
        {
            reasons.Add("Average grade below 60");
        }
        else if (averageGrade < 70)
        {
            reasons.Add("Average grade below 70");
        }

        var status = StudentRiskStatus.Good;

        if ((student.Grades.Any() && averageGrade < 60) ||
            (attendanceTotal > 0 && attendanceRate < 70))
        {
            status = StudentRiskStatus.AtRisk;
        }
        else if (!student.Grades.Any() ||
                 attendanceTotal == 0 ||
                 averageGrade < 70 ||
                 attendanceRate < 85)
        {
            status = StudentRiskStatus.AttentionNeeded;
        }

        return new StudentRiskResult(
            status,
            (double)averageGrade,
            attendanceRate,
            reasons);
    }
}

public enum StudentRiskStatus
{
    Good,
    AttentionNeeded,
    AtRisk
}

public record StudentRiskResult(
    StudentRiskStatus Status,
    double AverageGrade,
    double AttendanceRate,
    IReadOnlyList<string> Reasons)
{
    public string Label => Status switch
    {
        StudentRiskStatus.Good => "Good",
        StudentRiskStatus.AttentionNeeded => "Attention Needed",
        StudentRiskStatus.AtRisk => "At Risk",
        _ => "Unknown"
    };

    public string CssClass => Status switch
    {
        StudentRiskStatus.Good => "risk-good",
        StudentRiskStatus.AttentionNeeded => "risk-attention",
        StudentRiskStatus.AtRisk => "risk-danger",
        _ => string.Empty
    };

    public string IconClass => Status switch
    {
        StudentRiskStatus.Good => "bi-check-circle-fill",
        StudentRiskStatus.AttentionNeeded => "bi-exclamation-triangle-fill",
        StudentRiskStatus.AtRisk => "bi-exclamation-octagon-fill",
        _ => "bi-question-circle-fill"
    };
}
