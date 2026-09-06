namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Resolves the physical folder uploaded files (currently just the site
/// logo) are written to and served from. In production this points
/// outside the versioned release directory (via the Uploads:Path
/// configuration key, set to a persistent shared folder) so uploads
/// survive redeploys; left unset, it defaults to wwwroot/uploads, which
/// is fine for local development's single, long-lived checkout.
/// </summary>
public class UploadsPathProvider
{
    public string PhysicalPath { get; }

    public UploadsPathProvider(string physicalPath)
    {
        PhysicalPath = physicalPath;
        Directory.CreateDirectory(PhysicalPath);
    }
}
