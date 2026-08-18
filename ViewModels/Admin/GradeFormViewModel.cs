using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class GradeFormViewModel
{
    public int Id { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Range(1, 5, ErrorMessage = "Grade must be between 1 and 5")]
    public int Value { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }
}
