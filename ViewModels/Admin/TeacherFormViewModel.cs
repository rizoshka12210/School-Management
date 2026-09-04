using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class TeacherFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal HourlyRate { get; set; }


    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public List<int> GroupIds { get; set; } = new();

    public List<int> SubjectIds { get; set; } = new();
}