namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// An admin-scheduled request for a parent to come to school at a
/// specific time. Unlike the rest of NotificationService, this is real
/// data an admin authored - not something computed from existing tables -
/// so it needs its own table.
/// </summary>
public class ParentSummon
{
    public int Id { get; set; }

    public int ParentId { get; set; }

    public Parent Parent { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
