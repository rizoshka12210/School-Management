namespace SchoolManagementSystem.Web.Models.Entities;

public class StudentComment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string Text { get; set; } = string.Empty;
}
