namespace SchoolManagementSystem.Web.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Parent = "Parent";

    /// <summary>
    /// Read-only counterpart to Admin (e.g. a school principal): sees
    /// every page in the Admin area but every create/edit/delete action
    /// stays restricted to Roles.Admin alone.
    /// </summary>
    public const string Director = "Director";

    /// <summary>Admin and Director together, for controllers/actions that should be viewable by both.</summary>
    public const string AdminAndDirector = Admin + "," + Director;
}