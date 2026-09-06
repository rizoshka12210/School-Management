using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Reads/writes the single editable-branding row (site name + logo).
/// Cached briefly since _Layout and the login page both read it on
/// every request.
/// </summary>
public class SiteSettingsService
{
    private const string CacheKey = "SiteSettings";
    private static readonly string[] AllowedLogoExtensions =
        { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxLogoSizeBytes = 3 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly UploadsPathProvider _uploadsPathProvider;

    public SiteSettingsService(
        AppDbContext context,
        IMemoryCache cache,
        UploadsPathProvider uploadsPathProvider)
    {
        _context = context;
        _cache = cache;
        _uploadsPathProvider = uploadsPathProvider;
    }

    public async Task<SiteSettings> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) &&
            cached != null)
        {
            return cached;
        }

        var settings = await _context.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(5));

        return settings;
    }

    /// <returns>
    /// null on success, or an error message if the logo file was
    /// rejected (name/size validation only - the caller still owns
    /// ModelState).
    /// </returns>
    public string? ValidateLogo(IFormFile logo)
    {
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

        if (!AllowedLogoExtensions.Contains(extension))
        {
            return "Only PNG, JPG, SVG or WEBP images are allowed.";
        }

        if (logo.Length > MaxLogoSizeBytes)
        {
            return "The logo file must be 3 MB or smaller.";
        }

        return null;
    }

    public async Task UpdateAsync(string siteName, IFormFile? logo)
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
        }

        settings.SiteName = siteName.Trim();

        if (logo != null && logo.Length > 0)
        {
            var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
            var fileName = "logo" + extension;
            var filePath = Path.Combine(_uploadsPathProvider.PhysicalPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            settings.LogoPath = "/uploads/" + fileName;
        }

        await _context.SaveChangesAsync();

        _cache.Remove(CacheKey);
    }
}
