using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<ExamGrade> ExamGrades => Set<ExamGrade>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<ActivityLogEntry> ActivityLogEntries => Set<ActivityLogEntry>();
    public DbSet<ParentSummon> ParentSummons => Set<ParentSummon>();
    public DbSet<TeacherNotice> TeacherNotices => Set<TeacherNotice>();
    public DbSet<BigExam> BigExams => Set<BigExam>();
    public DbSet<BigExamGrade> BigExamGrades => Set<BigExamGrade>();
    public DbSet<ExamBlacklistThreshold> ExamBlacklistThresholds => Set<ExamBlacklistThreshold>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Parent>()
            .HasMany(p => p.Students)
            .WithMany(s => s.Parents)
            .UsingEntity(j => j.ToTable("ParentStudents"));

        builder.Entity<Teacher>()
            .HasMany(t => t.Groups)
            .WithMany(g => g.Teachers)
            .UsingEntity(j => j.ToTable("TeacherGroups"));

        builder.Entity<Teacher>()
            .HasMany(t => t.Subjects)
            .WithMany(s => s.Teachers)
            .UsingEntity(j => j.ToTable("TeacherSubjects"));

        builder.Entity<Student>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft-deleted students stay in the database (grades, attendance
        // and comments are kept) but are hidden from every normal query.
        // Use _context.Students.IgnoreQueryFilters() to see them.
        builder.Entity<Student>()
            .HasQueryFilter(s => !s.IsDeleted);

        builder.Entity<Parent>()
            .HasOne(p => p.ApplicationUser)
            .WithOne()
            .HasForeignKey<Parent>(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Parent>()
            .HasIndex(p => p.ApplicationUserId)
            .IsUnique();

        builder.Entity<ParentSummon>()
            .HasOne(s => s.Parent)
            .WithMany()
            .HasForeignKey(s => s.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeacherNotice>()
            .HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Teacher>()
            .HasOne(t => t.ApplicationUser)
            .WithOne()
            .HasForeignKey<Teacher>(t => t.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Teacher>()
            .HasIndex(t => t.ApplicationUserId)
            .IsUnique();

        builder.Entity<Lesson>()
            .HasOne(l => l.Group)
            .WithMany(g => g.Lessons)
            .HasForeignKey(l => l.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Lesson>()
            .HasOne(l => l.Teacher)
            .WithMany(t => t.Lessons)
            .HasForeignKey(l => l.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Lesson>()
            .HasOne(l => l.Subject)
            .WithMany(s => s.Lessons)
            .HasForeignKey(l => l.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Schedules)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.Teacher)
            .WithMany(t => t.Schedules)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.Subject)
            .WithMany(sub => sub.Schedules)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attendance>()
            .HasOne(a => a.Lesson)
            .WithMany(l => l.Attendances)
            .HasForeignKey(a => a.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attendance>()
            .HasIndex(a => new { a.StudentId, a.LessonId })
            .IsUnique();

        builder.Entity<Grade>()
            .HasOne(g => g.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Grade>()
            .HasOne(g => g.Teacher)
            .WithMany(t => t.Grades)
            .HasForeignKey(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Grade>()
            .HasOne(g => g.Subject)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Grade>()
            .HasOne(g => g.Lesson)
            .WithMany(l => l.Grades)
            .HasForeignKey(g => g.LessonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExamGrade>()
            .Ignore(e => e.Average);

        // Not unique: ExamGrade is append-only history, so a student can
        // have several rows for the same subject over time. The latest
        // one per (StudentId, SubjectId) is always the current result.
        builder.Entity<ExamGrade>()
            .HasIndex(e => new { e.StudentId, e.SubjectId });

        builder.Entity<ExamGrade>()
            .HasOne(e => e.Student)
            .WithMany(s => s.ExamGrades)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamGrade>()
            .HasOne(e => e.Subject)
            .WithMany()
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExamGrade>()
            .HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExamGrade>()
            .HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BigExamGrade>()
            .Ignore(e => e.WeightedScore);

        // Not unique: BigExamGrade is append-only history, same as
        // ExamGrade - a student can have several rows for the same big
        // exam and subject over time, with the latest one being the
        // current score.
        builder.Entity<BigExamGrade>()
            .HasIndex(e => new { e.BigExamId, e.StudentId, e.SubjectId });

        builder.Entity<BigExamGrade>()
            .HasOne(e => e.BigExam)
            .WithMany(b => b.Grades)
            .HasForeignKey(e => e.BigExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BigExamGrade>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BigExamGrade>()
            .HasOne(e => e.Subject)
            .WithMany()
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BigExamGrade>()
            .HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BigExamGrade>()
            .HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExamBlacklistThreshold>()
            .HasIndex(t => new { t.GroupId, t.SubjectId })
            .IsUnique();

        builder.Entity<ExamBlacklistThreshold>()
            .HasOne(t => t.Group)
            .WithMany()
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamBlacklistThreshold>()
            .HasOne(t => t.Subject)
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
