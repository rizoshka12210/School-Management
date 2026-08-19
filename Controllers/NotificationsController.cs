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
        return View(notifications);
    }
}
