using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.ModelBinding;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

/// <summary>
/// One group's Big Exam sheet - every subject taught at the center as
/// a column, one row per student, matching the school's paper grading
/// sheet (raw score per subject, converted to a weighted score using
/// each subject's own weight, summed into a Балли умумӣ grand total).
/// </summary>
public class BigExamGroupSheetViewModel
{
    public int BigExamId { get; set; }

    public string BigExamTitle { get; set; } = string.Empty;

    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public List<BigExamSubjectColumnViewModel> Subjects { get; set; } = new();

    public List<BigExamGroupSheetRowViewModel> Rows { get; set; } = new();

    public List<BigExamHistoryEntryViewModel> History { get; set; } = new();
}

public class BigExamSubjectColumnViewModel
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public decimal MaxRawScore { get; set; }

    public decimal MaxWeightedScore { get; set; }
}

public class BigExamGroupSheetRowViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public List<BigExamCellViewModel> Cells { get; set; } = new();

    public decimal TotalWeightedScore =>
        Cells.Where(c => c.WeightedScore.HasValue).Sum(c => c.WeightedScore!.Value);
}

public class BigExamCellViewModel
{
    public int SubjectId { get; set; }

    [Range(0, 1000, ErrorMessage = "Score must be zero or a positive number")]
    [ModelBinder(typeof(DecimalCommaModelBinder))]
    public decimal? RawScore { get; set; }

    public decimal MaxRawScore { get; set; }

    public decimal MaxWeightedScore { get; set; }

    public decimal? WeightedScore =>
        RawScore.HasValue && MaxRawScore > 0
            ? Math.Round(RawScore.Value / MaxRawScore * MaxWeightedScore, 3)
            : null;
}

public class BigExamGroupSheetSaveViewModel
{
    public int BigExamId { get; set; }

    public int GroupId { get; set; }

    public List<BigExamGroupSheetRowViewModel> Rows { get; set; } = new();
}

public class BigExamHistoryEntryViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public decimal? RawScore { get; set; }

    public decimal? WeightedScore { get; set; }

    public string? Comment { get; set; }

    public string GradedBy { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public bool IsCurrent { get; set; }
}
