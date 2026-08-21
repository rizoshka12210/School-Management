namespace SchoolManagementSystem.Web.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public string Icon { get; set; } = "🔔";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DateTime? OccurredAt { get; set; }
    public string Kind { get; set; } = "info";
    public string FingerprintKey { get; set; } = string.Empty;
}
