using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using Student_Management_System.Models;

namespace StudentManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // EXISTING TABLES
        // =====================================================

        public DbSet<Student> Students { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Teacher> Teachers { get; set; }

        public DbSet<Semester> Semesters { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Result> Results { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }


        // =====================================================
        // ASSIGNMENT TABLES
        // =====================================================

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }


        // =====================================================
        // NEW TABLES (Student / Teacher / Admin module upgrade)
        // =====================================================

        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<ClassSchedule> ClassSchedules { get; set; }

        public DbSet<Fee> Fees { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Document> Documents { get; set; }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<ContactMessage> ContactMessages { get; set; }

        public DbSet<CourseMaterial> CourseMaterials { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =====================================================
            // APPLICATION USER → STUDENT
            // =====================================================

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Student)
                .WithOne()
                .HasForeignKey<ApplicationUser>(
                    u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);


            // =====================================================
            // APPLICATION USER → TEACHER
            // =====================================================

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Teacher)
                .WithOne()
                .HasForeignKey<ApplicationUser>(
                    u => u.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);


            // =====================================================
            // STUDENT → DEPARTMENT
            // =====================================================

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // COURSE → DEPARTMENT
            // =====================================================

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Courses)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // TEACHER → DEPARTMENT
            // =====================================================

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Department)
                .WithMany(d => d.Teachers)
                .HasForeignKey(t => t.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ENROLLMENT → STUDENT
            // =====================================================

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ENROLLMENT → COURSE
            // =====================================================

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ENROLLMENT → SEMESTER
            // =====================================================

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Semester)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ATTENDANCE → STUDENT
            // =====================================================

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ATTENDANCE → COURSE
            // =====================================================

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ATTENDANCE → SEMESTER
            // =====================================================

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Semester)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ATTENDANCE UNIQUE INDEX
            // =====================================================

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.StudentId,
                    a.CourseId,
                    a.AttendanceDate
                })
                .IsUnique();


            // =====================================================
            // RESULT → STUDENT
            // =====================================================

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Student)
                .WithMany(s => s.Results)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // RESULT → COURSE
            // =====================================================

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Course)
                .WithMany()
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // RESULT DECIMAL PRECISION
            // =====================================================

            modelBuilder.Entity<Result>()
                .Property(r => r.InternalMarks)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Result>()
                .Property(r => r.PracticalMarks)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Result>()
                .Property(r => r.FinalMarks)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Result>()
                .Property(r => r.TotalMarks)
                .HasPrecision(5, 2);


            // =====================================================
            // RESULT UNIQUE INDEX
            // =====================================================

            modelBuilder.Entity<Result>()
                .HasIndex(r => new
                {
                    r.StudentId,
                    r.CourseId
                })
                .IsUnique();


            // =====================================================
            // STUDENT UNIQUE INDEXES
            // =====================================================

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.RegistrationNumber)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();


            // =====================================================
            // DEPARTMENT UNIQUE INDEXES
            // =====================================================

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.DepartmentName)
                .IsUnique();

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.DepartmentCode)
                .IsUnique();


            // =====================================================
            // COURSE UNIQUE INDEX
            // =====================================================

            modelBuilder.Entity<Course>()
                .HasIndex(c => c.CourseCode)
                .IsUnique();


            // =====================================================
            // ASSIGNMENT → COURSE
            // =====================================================

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ASSIGNMENT → TEACHER
            // =====================================================

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // SUBMISSION → ASSIGNMENT
            // =====================================================

            modelBuilder.Entity<AssignmentSubmission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // SUBMISSION → STUDENT
            // =====================================================

            modelBuilder.Entity<AssignmentSubmission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // SUBMISSION UNIQUE INDEX
            // One submission per student per assignment
            // =====================================================

            modelBuilder.Entity<AssignmentSubmission>()
                .HasIndex(s => new
                {
                    s.AssignmentId,
                    s.StudentId
                })
                .IsUnique();

            modelBuilder.Entity<AssignmentSubmission>()
                .Property(s => s.Marks)
                .HasPrecision(5, 2);


            // =====================================================
            // NOTIFICATION → APPLICATION USER
            // =====================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // ANNOUNCEMENT → APPLICATION USER (AUTHOR)
            // =====================================================

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ANNOUNCEMENT → COURSE
            // =====================================================

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // CLASS SCHEDULE → COURSE / TEACHER / SEMESTER
            // =====================================================

            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Course)
                .WithMany()
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Teacher)
                .WithMany()
                .HasForeignKey(cs => cs.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Semester)
                .WithMany()
                .HasForeignKey(cs => cs.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // FEE → STUDENT / SEMESTER
            // =====================================================

            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Student)
                .WithMany()
                .HasForeignKey(f => f.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Semester)
                .WithMany()
                .HasForeignKey(f => f.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fee>()
                .Property(f => f.TotalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Fee>()
                .HasIndex(f => new { f.StudentId, f.SemesterId })
                .IsUnique();


            // =====================================================
            // PAYMENT → FEE
            // =====================================================

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Fee)
                .WithMany(f => f.Payments)
                .HasForeignKey(p => p.FeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasPrecision(10, 2);


            // =====================================================
            // DOCUMENT → STUDENT
            // =====================================================

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // LEAVE REQUEST → STUDENT / COURSE
            // =====================================================

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Student)
                .WithMany()
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Course)
                .WithMany()
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.SetNull);


            // =====================================================
            // CONTACT MESSAGE → STUDENT / TEACHER / COURSE / PARENT
            // =====================================================

            modelBuilder.Entity<ContactMessage>()
                .HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContactMessage>()
                .HasOne(m => m.Teacher)
                .WithMany()
                .HasForeignKey(m => m.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContactMessage>()
                .HasOne(m => m.Course)
                .WithMany()
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ContactMessage>()
                .HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // COURSE MATERIAL → COURSE / TEACHER
            // =====================================================

            modelBuilder.Entity<CourseMaterial>()
                .HasOne(cm => cm.Course)
                .WithMany()
                .HasForeignKey(cm => cm.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseMaterial>()
                .HasOne(cm => cm.Teacher)
                .WithMany()
                .HasForeignKey(cm => cm.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}