using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

public class AttendanceService
{
    private readonly AppDbContext _context;

    public AttendanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Attendance>> GetAllAsync()
    {
        return await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Teacher)
                    .ThenInclude(t => t.ApplicationUser)
            .OrderByDescending(a => a.Lesson.StartTime)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetByStudentAsync(
        int studentId)
    {
        return await _context.Attendances
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Student)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Group)
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Subject)
            .OrderByDescending(a => a.Lesson.StartTime)
            .ToListAsync();
    }
}