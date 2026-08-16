namespace SchoolManagementSystem.Web.ViewModels.Parent;

public class SubjectWithTeachers
{
    public string SubjectName { get; set; } = string.Empty;

    public List<string> TeacherNames { get; set; } = new();
}
