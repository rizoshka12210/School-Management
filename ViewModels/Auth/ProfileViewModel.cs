using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Auth;

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    public string? PhoneNumber { get; set; }
}
