using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace VirtualClassroom2.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision for Marks
            builder.Entity<Submission>()
                .Property(s => s.Marks)
                .HasPrecision(18, 2);

            // Configure relationships - Remove Cascade delete to avoid multiple paths
            builder.Entity<Enrollment>()
                .HasOne(e => e.Classroom)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassroomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Content>()
                .HasOne(c => c.Classroom)
                .WithMany(cr => cr.Contents)
                .HasForeignKey(c => c.ClassroomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Content>()
                .HasOne(c => c.UploadedBy)
                .WithMany(u => u.ContentsUploaded)
                .HasForeignKey(c => c.UploadedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Assignment>()
                .HasOne(a => a.Classroom)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassroomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Assignment>()
                .HasOne(a => a.CreatedBy)
                .WithMany(u => u.AssignmentsCreated)
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Announcement>()
                .HasOne(a => a.Classroom)
                .WithMany(c => c.Announcements)
                .HasForeignKey(a => a.ClassroomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Announcement>()
                .HasOne(a => a.PostedBy)
                .WithMany(u => u.AnnouncementsPosted)
                .HasForeignKey(a => a.PostedById)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}