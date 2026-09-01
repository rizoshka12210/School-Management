using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.ModelBinding;

namespace SchoolManagementSystem.Web.ViewModels.Teacher;

public class GradeEntryViewModel
{
    public int LessonId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime LessonDate { get; set; }

    public List<GradeRow> Students { get; set; } = new();
}

public class GradeRow
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    [ModelBinder(typeof(DecimalCommaModelBinder))]
    public decimal? Value { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }
}
