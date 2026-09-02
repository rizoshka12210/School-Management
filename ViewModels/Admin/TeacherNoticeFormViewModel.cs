using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class TeacherNoticeFormViewModel
{
    public int TeacherId { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
}
