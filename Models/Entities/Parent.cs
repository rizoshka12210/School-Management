using SchoolManagementSystem.Web.Models.Enums;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Models.Entities;

public class Parent
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    /// <summary>Mother/Father/Guardian - so a child's two parent accounts can be told apart at a glance.</summary>
    public ParentRelation Relation { get; set; } = ParentRelation.Unspecified;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}