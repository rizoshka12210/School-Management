using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

[Area("Parent")]
[Authorize(Roles = Roles.Parent)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}