using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Models.Entities;

public class Teacher
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public decimal HourlyRate { get; set; }

    public ICollection<Group> Groups { get; set; } = new List<Group>();

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}