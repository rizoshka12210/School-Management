using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

public class GradeService
{
    private readonly AppDbContext _context;

    public GradeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Grade>> GetAllAsync()
    {
        return await _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(g => g.Lesson)
            .OrderByDescending(g => g.Date)
            .ToListAsync();
    }

    public async Task<List<Grade>> GetByStudentAsync(
        int studentId)
    {
        return await _context.Grades
            .Where(g => g.StudentId == studentId)
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Teacher)
                .ThenInclude(t => t.ApplicationUser)
            .Include(g => g.Lesson)
            .OrderByDescending(g => g.Date)
            .ToListAsync();
    }
}