using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class BigExamFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
}
