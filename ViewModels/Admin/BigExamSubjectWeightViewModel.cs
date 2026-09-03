namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class BigExamSubjectWeightViewModel
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public decimal MaxRawScore { get; set; }

    public decimal MaxWeightedScore { get; set; }
}
