namespace SchoolManagementSystem.Web.ViewModels.Teacher;

public class GradesGroupViewModel
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int StudentsCount { get; set; }

    public List<GradesSubjectViewModel> Subjects { get; set; } = new();
}

public class GradesSubjectViewModel
{
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public int StudentsGraded { get; set; }
}
