using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

public class TopicCalendarService
{
    private readonly AppDbContext _context;

    public TopicCalendarService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Lesson>> GetTeacherTopicsAsync(int teacherId)
    {
        return await _context.Lessons
            .Where(l => l.TeacherId == teacherId)
            .Include(l => l.Group)
            .Include(l => l.Subject)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();
    }

    public async Task<List<Lesson>> GetStudentTopicsAsync(int studentId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null || student.GroupId == null)
        {
            return new List<Lesson>();
        }

        return await _context.Lessons
            .Where(l =>
                l.GroupId == student.GroupId &&
                l.Topic != null)
            .Include(l => l.Subject)
            .Include(l => l.Group)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();
    }
}