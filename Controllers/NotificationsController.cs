using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Services;

namespace SchoolManagementSystem.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var notifications = await _notificationService.GetForUserAsync(User, 30);
        var fingerprint = await _notificationService.GetCurrentFingerprintAsync(User);

        if (!string.IsNullOrWhiteSpace(fingerprint))
        {
            Response.Cookies.Append(
                NotificationService.SeenCookieName,
                fingerprint,
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });
        }
        else
        {
            Response.Cookies.Delete(NotificationService.SeenCookieName);
        }

        return View(notifications);
    }
}
