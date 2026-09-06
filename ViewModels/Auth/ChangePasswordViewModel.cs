using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Auth;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "The new password and confirmation do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
