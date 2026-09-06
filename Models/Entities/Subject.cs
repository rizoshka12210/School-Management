namespace SchoolManagementSystem.Web.Models.Entities;

public class Subject
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

    /// <summary>
    /// The single teacher (if any) the admin has designated to manage the
    /// schedule for this subject alongside Admin. Toggled exclusively via
    /// Admin > Schedule > Head Teacher Access - at most one teacher per
    /// subject at a time.
    /// </summary>
    public int? HeadTeacherId { get; set; }

    public Teacher? HeadTeacher { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();

    /// <summary>
    /// The raw score a Big Exam paper for this subject is marked out of
    /// (e.g. 40). Used with <see cref="BigExamMaxWeightedScore"/> to
    /// convert a student's raw score into the subject's weighted score:
    /// RawScore / BigExamMaxRawScore * BigExamMaxWeightedScore.
    /// </summary>
    public decimal BigExamMaxRawScore { get; set; } = 100;

    /// <summary>
    /// The subject's importance weight in the Big Exam total (e.g. 175
    /// for Chemistry vs 100 for Physics) - different subjects can carry
    /// different weight toward Балли умумӣ (the Big Exam grand total).
    /// </summary>
    public decimal BigExamMaxWeightedScore { get; set; } = 100;
}