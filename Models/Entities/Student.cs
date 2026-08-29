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

    /// <summary>
    /// Students are soft-deleted so their history (grades, attendance,
    /// comments) stays intact and visible in the "deleted" list instead
    /// of being lost. A global query filter hides them from every normal
    /// query; the Admin "deleted students" view explicitly bypasses it.
    /// </summary>
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
