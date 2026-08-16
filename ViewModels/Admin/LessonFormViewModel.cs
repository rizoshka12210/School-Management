using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class LessonFormViewModel
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartTime { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EndTime { get; set; }

    [StringLength(300)]
    public string? Topic { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a group")]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a teacher")]
    public int TeacherId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a subject")]
    public int SubjectId { get; set; }
}