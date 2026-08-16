using Microsoft.AspNetCore.Identity;

namespace SchoolManagementSystem.Web.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}