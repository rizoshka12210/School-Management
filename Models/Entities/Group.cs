namespace SchoolManagementSystem.Web.Models.Entities;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new List<Student>();

    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}