using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Web.Models.Enums;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class AttendanceFormViewModel
{
    public int Id { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime LessonDate { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
