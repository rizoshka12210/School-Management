namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// A short message an admin sends directly to one teacher (a remark,
/// a note about something to improve, feedback) - it shows up in that
/// teacher's notifications, same as a ParentSummon does for a parent.
/// Real data an admin authored, not something computed from existing
/// tables, so it needs its own table.
/// </summary>
public class TeacherNotice
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
