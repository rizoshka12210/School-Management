using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class CalendarEventFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Event title is required")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event date is required")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [StringLength(500)]
    public string? Description { get; set; }
}
