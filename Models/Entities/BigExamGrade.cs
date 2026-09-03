namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// One student's score for one <see cref="BigExam"/>. Append-only, same
/// design as <see cref="ExamGrade"/>: a new row is added whenever the
/// score actually changes instead of overwriting the previous one, so
/// the full grading history survives. The row with the latest
/// <see cref="UpdatedAt"/> per (BigExamId, StudentId) is the current
/// score. GroupId is a snapshot of the student's group at grading time,
/// so group rankings stay correct even if a student later moves to a
/// different group.
/// </summary>
public class BigExamGrade
{
    public int Id { get; set; }

    public int BigExamId { get; set; }

    public BigExam BigExam { get; set; } = null!;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int GroupId { get; set; }

    public Group Group { get; set; } = null!;

    public decimal? Score { get; set; }

    public string? Comment { get; set; }

    /// <summary>Who entered this row - the designated teacher, or null when Admin did.</summary>
    public int? TeacherId { get; set; }

    public Teacher? Teacher { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
