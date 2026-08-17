using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int StudentsCount { get; set; }

    public int TeachersCount { get; set; }

    public int GroupsCount { get; set; }

    public int SubjectsCount { get; set; }

    public int TodayLessonsCount { get; set; }

    public List<Lesson> TodayLessons { get; set; } = new();

    public List<Student> RecentStudents { get; set; } = new();
}