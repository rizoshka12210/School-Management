using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}