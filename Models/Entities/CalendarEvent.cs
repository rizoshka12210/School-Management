namespace SchoolManagementSystem.Web.Models.Entities;

public class CalendarEvent
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Description { get; set; }
}
