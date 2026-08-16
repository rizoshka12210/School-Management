using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Teacher.Controllers;

/// <summary>
/// Shared base for every controller in the Teacher area.
/// [Area] and [Authorize] are inherited by every derived controller,
/// and GetTeacherIdAsync() is the single source of truth for "who is
/// the signed-in teacher" so ownership checks can never be skipped.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public abstract class TeacherControllerBase : Controller
{
    protected readonly AppDbContext Context;
    protected readonly OwnershipHelper Ownership;

    protected TeacherControllerBase(
        AppDbContext context,
        OwnershipHelper ownership)
    {
        Context = context;
        Ownership = ownership;
    }

    protected async Task<int?> GetTeacherIdAsync()
    {
        return await Ownership.GetCurrentTeacherIdAsync(User);
    }
}
