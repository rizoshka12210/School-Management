using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Tests;

public class StudentRiskServiceTests
{
    private readonly StudentRiskService _service = new();

    [Fact]
    public void Evaluate_ReturnsGood_WhenGradesAndAttendanceAreStrong()
    {
        var student = new Student
        {
            Grades =
            {
                new Grade { Value = 5 },
                new Grade { Value = 4 }
            },
            Attendances =
            {
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Late }
            }
        };

        var result = _service.Evaluate(student);

        Assert.Equal(StudentRiskStatus.Good, result.Status);
        Assert.Equal(4.5, result.AverageGrade);
        Assert.Equal(100, result.AttendanceRate);
    }

    [Fact]
    public void Evaluate_ReturnsAtRisk_WhenAverageGradeIsBelowThree()
    {
        var student = new Student
        {
            Grades =
            {
                new Grade { Value = 2 },
                new Grade { Value = 3 }
            },
            Attendances =
            {
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Present }
            }
        };

        var result = _service.Evaluate(student);

        Assert.Equal(StudentRiskStatus.AtRisk, result.Status);
        Assert.Contains("Average grade below 3.0", result.Reasons);
    }

    [Fact]
    public void Evaluate_ReturnsAtRisk_WhenAttendanceIsBelowSeventyPercent()
    {
        var student = new Student
        {
            Grades =
            {
                new Grade { Value = 4 },
                new Grade { Value = 5 }
            },
            Attendances =
            {
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Absent },
                new Attendance { Status = AttendanceStatus.Absent }
            }
        };

        var result = _service.Evaluate(student);

        Assert.Equal(StudentRiskStatus.AtRisk, result.Status);
        Assert.True(result.AttendanceRate < 70);
    }

    [Fact]
    public void Evaluate_ReturnsAttentionNeeded_WhenThereIsNoData()
    {
        var result = _service.Evaluate(new Student());

        Assert.Equal(StudentRiskStatus.AttentionNeeded, result.Status);
        Assert.Contains("No attendance data yet", result.Reasons);
        Assert.Contains("No grades recorded yet", result.Reasons);
    }
}
