namespace SchoolManagementSystem.Web.Models.Entities;

public class ActivityLogEntry
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Message { get; set; } = string.Empty;
}
