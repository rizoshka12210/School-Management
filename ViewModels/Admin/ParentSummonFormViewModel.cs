using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class ParentSummonFormViewModel
{
    public int ParentId { get; set; }

    public string ParentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date and time are required")]
    public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

    [StringLength(500)]
    public string? Message { get; set; }
}
