using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

public class ActivityLogService
{
    private readonly AppDbContext _context;

    public ActivityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string message)
    {
        _context.ActivityLogEntries.Add(new ActivityLogEntry
        {
            Message = message
        });

        await _context.SaveChangesAsync();
    }
}
