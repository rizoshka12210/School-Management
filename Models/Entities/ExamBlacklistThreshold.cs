namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// One row per (Group, Subject) combination the exam sheet is graded
/// for - mirrors the Big Exam's per-exam blacklist threshold
/// (<see cref="BigExam.BlacklistThreshold"/>), but since a regular exam
/// sheet has no single "exam" to hang a threshold off, it is keyed by
/// the group/subject pair instead. Students whose latest exam average
/// for that subject falls below the threshold are highlighted in the
/// sheet and history views. Settable by the teacher who teaches that
/// group/subject, or by Admin.
/// </summary>
public class ExamBlacklistThreshold
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public decimal Threshold { get; set; }
}
