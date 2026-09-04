using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Web.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or phone number is required")]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
