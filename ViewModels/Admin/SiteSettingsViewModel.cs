using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class SiteSettingsViewModel
{
    [Required(ErrorMessage = "Site name is required")]
    public string SiteName { get; set; } = string.Empty;

    public IFormFile? Logo { get; set; }

    public string? LogoPath { get; set; }
}
