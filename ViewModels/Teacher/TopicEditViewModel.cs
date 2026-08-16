using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Teacher;

public class TopicEditViewModel
{
    public int LessonId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime LessonDate { get; set; }

    [StringLength(300)]
    public string? Topic { get; set; }
}
