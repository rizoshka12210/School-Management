namespace SchoolManagementSystem.Web.Models.Entities;

public class StudentPayment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
