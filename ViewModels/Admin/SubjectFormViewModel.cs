using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Subject name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<int> TeacherIds { get; set; } = new();
}