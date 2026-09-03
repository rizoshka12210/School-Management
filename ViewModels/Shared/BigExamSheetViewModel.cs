using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.ModelBinding;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

public class BigExamSheetViewModel
{
    public int BigExamId { get; set; }

    public string BigExamTitle { get; set; } = string.Empty;

    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public List<BigExamSheetRowViewModel> Rows { get; set; } = new();

    public List<BigExamHistoryEntryViewModel> History { get; set; } = new();
}

public class BigExamSheetRowViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    [ModelBinder(typeof(DecimalCommaModelBinder))]
    public decimal? Score { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }
}

public class BigExamSheetSaveViewModel
{
    public int BigExamId { get; set; }

    public int GroupId { get; set; }

    public int SubjectId { get; set; }

    public List<BigExamSheetRowViewModel> Rows { get; set; } = new();
}

public class BigExamHistoryEntryViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public string? Comment { get; set; }

    public string GradedBy { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public bool IsCurrent { get; set; }
}
