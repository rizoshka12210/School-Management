namespace SchoolManagementSystem.Web.Models.Entities;

public class Student
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public int? GroupId { get; set; }

    public Group? Group { get; set; }

    public ICollection<Parent> Parents { get; set; } = new List<Parent>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}