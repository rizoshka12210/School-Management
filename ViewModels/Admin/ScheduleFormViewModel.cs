using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class ScheduleFormViewModel
{
    public int Id { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a group")]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a teacher")]
    public int TeacherId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a subject")]
    public int SubjectId { get; set; }
}