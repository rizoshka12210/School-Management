namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// One row per student per subject: two exam scores and their average,
/// matching the exam sheet teachers already keep outside the system
/// (Имтихони №1, Имтихони №2, Балли миёна). Unlike Grade, this is not
/// tied to a single lesson - it is the term's exam result for the
/// student in that subject.
/// </summary>
public class ExamGrade
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public decimal? Exam1 { get; set; }

    public decimal? Exam2 { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Average of whichever exam scores have been entered so far.
    /// Not stored - always derived from the current Exam1/Exam2 values.
    /// </summary>
    public decimal? Average
    {
        get
        {
            if (Exam1.HasValue && Exam2.HasValue)
            {
                return Math.Round((Exam1.Value + Exam2.Value) / 2, 2);
            }

            return Exam1 ?? Exam2;
        }
    }
}
