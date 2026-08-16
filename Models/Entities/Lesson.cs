namespace SchoolManagementSystem.Web.Models.Entities;

public class Lesson
{
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }


    public string? Topic { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();

    public ICollection<Grade> Grades { get; set; }
        = new List<Grade>();
}