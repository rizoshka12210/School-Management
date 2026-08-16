using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Models.Entities;

public class Parent
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;


    public ICollection<Student> Students { get; set; } = new List<Student>();
}