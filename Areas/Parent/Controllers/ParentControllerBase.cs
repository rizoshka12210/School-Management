using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;

namespace SchoolManagementSystem.Web.Areas.Parent.Controllers;

/// <summary>
/// Shared base for every controller in the Parent area.
/// ResolveStudentIdAsync is the single choke point every action must
/// go through before touching a child's data: it both defaults to the
/// parent's own child when no id is supplied, and rejects any id that
/// does not belong to the signed-in parent (IDOR protection).
/// </summary>
[Area("Parent")]
[Authorize(Roles = Roles.Parent)]
public abstract class ParentControllerBase : Controller
{
    protected readonly AppDbContext Context;
    protected readonly OwnershipHelper Ownership;

    protected ParentControllerBase(
        AppDbContext context,
        OwnershipHelper ownership)
    {
        Context = context;
        Ownership = ownership;
    }

    protected async Task<int?> GetParentIdAsync()
    {
        return await Ownership.GetCurrentParentIdAsync(User);
    }

    /// <summary>
    /// Returns the id of the student the current action should show.
    /// If <paramref name="studentId"/> is null, it resolves to the
    /// parent's first child. If it is set, it is only returned when
    /// the signed-in parent actually owns that student - otherwise
    /// null is returned and the caller must treat that as "forbidden".
    /// </summary>
    protected async Task<int?> ResolveStudentIdAsync(int? studentId)
    {
        var parentId = await GetParentIdAsync();

        if (parentId == null)
        {
            return null;
        }

        if (studentId.HasValue)
        {
            var owns = await Ownership.ParentOwnsStudentAsync(
                User,
                studentId.Value);

            return owns ? studentId : null;
        }

        return await Context.Parents
            .Where(p => p.Id == parentId)
            .SelectMany(p => p.Students)
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Every one of the signed-in parent's children, for highlighting
    /// "this is your child" in a shared list/ranking that shows every
    /// student (Big Exam rankings, the leaderboard) rather than just one.
    /// </summary>
    protected async Task<HashSet<int>> GetOwnedStudentIdsAsync()
    {
        var parentId = await GetParentIdAsync();

        if (parentId == null)
        {
            return new HashSet<int>();
        }

        return (await Context.Parents
                .Where(p => p.Id == parentId)
                .SelectMany(p => p.Students)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();
    }
}
