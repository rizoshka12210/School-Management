using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.ModelBinding;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

public class ExamSheetViewModel
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public List<ExamSheetRowViewModel> Rows { get; set; } = new();
}

public class ExamSheetRowViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    [ModelBinder(typeof(DecimalCommaModelBinder))]
    public decimal? Exam1 { get; set; }

    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    [ModelBinder(typeof(DecimalCommaModelBinder))]
    public decimal? Exam2 { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }

    public decimal? Average =>
        Exam1.HasValue && Exam2.HasValue
            ? Math.Round((Exam1.Value + Exam2.Value) / 2, 2)
            : Exam1 ?? Exam2;
}

public class ExamSheetSaveViewModel
{
    public int GroupId { get; set; }

    public int SubjectId { get; set; }

    public List<ExamSheetRowViewModel> Rows { get; set; } = new();
}
