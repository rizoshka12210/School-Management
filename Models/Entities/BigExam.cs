using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// A periodic, school-wide assessment ("Большой экзамен") held roughly
/// every two weeks - distinct from the regular per-subject exam sheets
/// teachers fill in daily (<see cref="ExamGrade"/>). Graded only by
/// Admin or the single teacher the admin has designated
/// (<see cref="Teacher.IsBigExamGrader"/>), with rankings computed both
/// within each group and across the whole school.
/// </summary>
public class BigExam
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BigExamGrade> Grades { get; set; } = new List<BigExamGrade>();
}
