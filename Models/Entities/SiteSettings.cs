namespace SchoolManagementSystem.Web.Models.Entities;

/// <summary>
/// Single-row table (there is always exactly one) holding the site's
/// editable branding: the display name shown in the header/login page
/// and browser tab, and an optional uploaded logo image.
/// </summary>
public class SiteSettings
{
    public int Id { get; set; }

    public string SiteName { get; set; } = "School Management";

    /// <summary>
    /// Request path (e.g. "/uploads/logo.png") the logo is served from,
    /// or null if no custom logo has been uploaded yet.
    /// </summary>
    public string? LogoPath { get; set; }
}
