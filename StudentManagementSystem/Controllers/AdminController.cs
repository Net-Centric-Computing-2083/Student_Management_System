using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Models;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.ViewModels;

namespace Student_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private static readonly HashSet<string> AllowedDocumentExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
            };

        private const long MaxDocumentSize = 10 * 1024 * 1024; // 10 MB

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // =========================================================
        // ADMIN DASHBOARD
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalTeachers = await _context.Teachers.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                TotalResults = await _context.Results.CountAsync(),
                TotalAttendance = await _context.Attendances.CountAsync(),
                PendingLeaveRequests = await _context.LeaveRequests
                    .CountAsync(l => l.CourseId == null && l.Status == "Pending"),
                NewContactMessages = await _context.ContactMessages
                    .CountAsync(m => m.Status == "New")
            };

            var recentAnnouncements = await _context.Announcements
                .Where(a => a.Scope == "System")
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();

            var recentLeaveRequests = await _context.LeaveRequests
                .Include(l => l.Student)
                .OrderByDescending(l => l.RequestDate)
                .Take(5)
                .ToListAsync();

            var recentMessages = await _context.ContactMessages
                .Include(m => m.Student)
                .Where(m => m.ParentMessageId == null)
                .OrderByDescending(m => m.SentDate)
                .Take(5)
                .ToListAsync();

            var activity = new List<AdminActivityItem>();

            activity.AddRange(recentAnnouncements.Select(a => new AdminActivityItem
            {
                When = a.CreatedDate,
                Description = $"Announcement posted: \"{a.Title}\""
            }));

            activity.AddRange(recentLeaveRequests.Select(l => new AdminActivityItem
            {
                When = l.RequestDate,
                Description = $"Leave request from {l.Student?.Name} ({l.Status})"
            }));

            activity.AddRange(recentMessages.Select(m => new AdminActivityItem
            {
                When = m.SentDate,
                Description = $"{m.Student?.Name} contacted a teacher: \"{m.Subject}\""
            }));

            viewModel.RecentActivity = activity
                .OrderByDescending(a => a.When)
                .Take(8)
                .ToList();

            return View(viewModel);
        }

        // =========================================================
        // MY PROFILE
        // =========================================================

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string FullName, string? PhoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ModelState.AddModelError(nameof(FullName), "Name is required.");
                return View(user);
            }

            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // =========================================================
        // TEACHER MANAGEMENT
        // =========================================================

        public async Task<IActionResult> Teachers(
            string? searchString,
            int? departmentId,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 8;
            page = Math.Max(page, 1);

            var teachers = _context.Teachers
                .Include(t => t.Department)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                teachers = teachers.Where(t =>
                    (t.Name ?? "").Contains(searchString) ||
                    (t.Email ?? "").Contains(searchString) ||
                    (t.Phone ?? "").Contains(searchString));
            }

            if (departmentId.HasValue)
                teachers = teachers.Where(t => t.DepartmentId == departmentId.Value);

            teachers = sortOrder switch
            {
                "name_desc" => teachers.OrderByDescending(t => t.Name),
                "email" => teachers.OrderBy(t => t.Email),
                "email_desc" => teachers.OrderByDescending(t => t.Email),
                _ => teachers.OrderBy(t => t.Name)
            };

            var total = await teachers.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var list = await teachers.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentDepartment"] = departmentId;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSort"] = sortOrder == "name_desc" ? "" : "name_desc";
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync(),
                "DepartmentId", "DepartmentName", departmentId);

            return View(list);
        }

        public async Task<IActionResult> TeacherDetails(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            if (teacher == null) return NotFound();

            ViewBag.Schedule = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .Where(cs => cs.TeacherId == id)
                .ToListAsync();

            return View(teacher);
        }

        public async Task<IActionResult> CreateTeacher()
        {
            await LoadDepartmentsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(Teacher teacher, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Password is required.");
            else if (password.Length < 6)
                ModelState.AddModelError("password", "Password must contain at least 6 characters.");

            if (!string.IsNullOrWhiteSpace(teacher.Email))
            {
                var teacherEmailExists = await _context.Teachers.AnyAsync(t => t.Email == teacher.Email);

                if (teacherEmailExists)
                    ModelState.AddModelError(nameof(Teacher.Email), "Email already exists.");

                var identityEmailExists = await _userManager.FindByEmailAsync(teacher.Email);

                if (identityEmailExists != null)
                    ModelState.AddModelError(nameof(Teacher.Email), "This email already has a login account.");
            }

            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == teacher.DepartmentId))
                ModelState.AddModelError(nameof(Teacher.DepartmentId), "Select a valid department.");

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(teacher.DepartmentId);
                return View(teacher);
            }

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var applicationUser = new ApplicationUser
            {
                UserName = teacher.Email,
                Email = teacher.Email,
                FullName = teacher.Name ?? string.Empty,
                EmailConfirmed = true,
                TeacherId = teacher.TeacherId
            };

            var identityResult = await _userManager.CreateAsync(applicationUser, password);

            if (!identityResult.Succeeded)
            {
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();

                foreach (var error in identityResult.Errors)
                    ModelState.AddModelError("password", error.Description);

                await LoadDepartmentsAsync(teacher.DepartmentId);
                return View(teacher);
            }

            var roleResult = await _userManager.AddToRoleAsync(applicationUser, "Teacher");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError("", $"Teacher role error: {error.Description}");

                await LoadDepartmentsAsync(teacher.DepartmentId);
                return View(teacher);
            }

            TempData["SuccessMessage"] =
                $"Teacher created successfully. Login Email: {teacher.Email}";

            return RedirectToAction(nameof(Teachers));
        }

        public async Task<IActionResult> EditTeacher(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            await LoadDepartmentsAsync(teacher.DepartmentId);
            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(int id, Teacher teacher)
        {
            if (id != teacher.TeacherId) return NotFound();

            if (await _context.Teachers.AnyAsync(t =>
                t.Email == teacher.Email && t.TeacherId != teacher.TeacherId))
            {
                ModelState.AddModelError(nameof(Teacher.Email), "Email already exists.");
            }

            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == teacher.DepartmentId))
                ModelState.AddModelError(nameof(Teacher.DepartmentId), "Select a valid department.");

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(teacher.DepartmentId);
                return View(teacher);
            }

            try
            {
                _context.Update(teacher);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Teachers.AnyAsync(t => t.TeacherId == id))
                    return NotFound();
                throw;
            }

            var identityUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.TeacherId == teacher.TeacherId);

            if (identityUser != null)
            {
                identityUser.FullName = teacher.Name ?? string.Empty;
                identityUser.Email = teacher.Email;
                identityUser.UserName = teacher.Email;
                await _userManager.UpdateAsync(identityUser);
            }

            TempData["SuccessMessage"] = "Teacher updated successfully.";
            return RedirectToAction(nameof(Teachers));
        }

        public async Task<IActionResult> DeleteTeacher(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            return teacher == null ? NotFound() : View(teacher);
        }

        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == id);
            if (teacher == null) return NotFound();

            var hasSchedule = await _context.ClassSchedules.AnyAsync(cs => cs.TeacherId == id);
            var hasAssignments = await _context.Assignments.AnyAsync(a => a.TeacherId == id);
            var hasMaterials = await _context.CourseMaterials.AnyAsync(m => m.TeacherId == id);
            var hasMessages = await _context.ContactMessages.AnyAsync(m => m.TeacherId == id);

            if (hasSchedule || hasAssignments || hasMaterials || hasMessages)
            {
                TempData["DeleteError"] =
                    "This teacher cannot be deleted while class schedules, assignments, course materials, or messages exist.";
                return RedirectToAction(nameof(DeleteTeacher), new { id });
            }

            var identityUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.TeacherId == teacher.TeacherId);

            if (identityUser != null)
                await _userManager.DeleteAsync(identityUser);

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Teacher deleted successfully.";
            return RedirectToAction(nameof(Teachers));
        }

        // =========================================================
        // USER & ROLE MANAGEMENT
        // =========================================================

        public async Task<IActionResult> Users(string? searchString)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                query = query.Where(u =>
                    (u.Email ?? "").Contains(searchString) ||
                    (u.FullName ?? "").Contains(searchString));
            }

            var users = await query.OrderBy(u => u.Email).ToListAsync();

            var rows = new List<AdminUserRowViewModel>();

            foreach (var user in users)
            {
                rows.Add(new AdminUserRowViewModel
                {
                    User = user,
                    Roles = await _userManager.GetRolesAsync(user),
                    IsLockedOut = await _userManager.IsLockedOutAsync(user)
                });
            }

            ViewData["CurrentSearch"] = searchString;
            ViewBag.AllRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["DeleteError"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            if (isLockedOut)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"{user.Email} has been reactivated.";
            }
            else
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"{user.Email} has been deactivated.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["DeleteError"] = "That role does not exist.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                TempData["DeleteError"] = $"{user.Email} already has the {role} role.";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = $"{role} role added to {user.Email}.";

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId && role == "Admin")
            {
                TempData["DeleteError"] = "You cannot remove your own Admin role.";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.RemoveFromRoleAsync(user, role);

            TempData["SuccessMessage"] = $"{role} role removed from {user.Email}.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // COURSE ASSIGNMENT / CLASS SCHEDULE (FULL CRUD)
        // =========================================================

        public async Task<IActionResult> ClassSchedules(
           int? teacherId,
           int? courseId,
           int? semesterId)
        {
            var query = _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Semester)
                .AsNoTracking()
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(cs => cs.TeacherId == teacherId.Value);

            if (courseId.HasValue)
                query = query.Where(cs => cs.CourseId == courseId.Value);

            if (semesterId.HasValue)
                query = query.Where(cs => cs.SemesterId == semesterId.Value);

            var list = await query
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            ViewBag.Teachers = new SelectList(
                await _context.Teachers
                    .OrderBy(t => t.Name)
                    .ToListAsync(),
                "TeacherId",
                "Name",
                teacherId);

            ViewBag.Courses = new SelectList(
                await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync(),
                "CourseId",
                "CourseName",
                courseId);

            ViewBag.Semesters = await BuildSemesterSelectListAsync(semesterId);

            return View(list);
        }

        public async Task<IActionResult> CreateClassSchedule()
        {
            await LoadScheduleDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClassSchedule(ClassSchedule schedule)
        {
            await ValidateClassScheduleAsync(schedule);

            if (!ModelState.IsValid)
            {
                await LoadScheduleDropdownsAsync(schedule.CourseId, schedule.TeacherId, schedule.SemesterId);
                return View(schedule);
            }

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class schedule created successfully.";
            return RedirectToAction(nameof(ClassSchedules));
        }

        public async Task<IActionResult> EditClassSchedule(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            await LoadScheduleDropdownsAsync(schedule.CourseId, schedule.TeacherId, schedule.SemesterId);
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClassSchedule(int id, ClassSchedule schedule)
        {
            if (id != schedule.ClassScheduleId) return NotFound();

            await ValidateClassScheduleAsync(schedule, schedule.ClassScheduleId);

            if (!ModelState.IsValid)
            {
                await LoadScheduleDropdownsAsync(schedule.CourseId, schedule.TeacherId, schedule.SemesterId);
                return View(schedule);
            }

            try
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ClassSchedules.AnyAsync(cs => cs.ClassScheduleId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Class schedule updated successfully.";
            return RedirectToAction(nameof(ClassSchedules));
        }

        public async Task<IActionResult> DeleteClassSchedule(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Semester)
                .FirstOrDefaultAsync(cs => cs.ClassScheduleId == id);

            return schedule == null ? NotFound() : View(schedule);
        }

        [HttpPost, ActionName("DeleteClassSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassScheduleConfirmed(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            _context.ClassSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class schedule deleted successfully.";
            return RedirectToAction(nameof(ClassSchedules));
        }

        // =========================================================
        // ASSIGNMENT MONITORING (VIEW-ONLY)
        // =========================================================

        public async Task<IActionResult> Assignments(int? teacherId, int? courseId)
        {
            var query = _context.Assignments
                .Include(a => a.Teacher)
                .Include(a => a.Course)
                .AsNoTracking()
                .AsQueryable();

            if (teacherId.HasValue) query = query.Where(a => a.TeacherId == teacherId.Value);
            if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId.Value);

            var list = await query.OrderByDescending(a => a.DueDate).ToListAsync();

            ViewBag.Teachers = new SelectList(
                await _context.Teachers.OrderBy(t => t.Name).ToListAsync(),
                "TeacherId", "Name", teacherId);

            ViewBag.Courses = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);

            return View(list);
        }

        public async Task<IActionResult> AssignmentDetails(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Teacher)
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            return assignment == null ? NotFound() : View(assignment);
        }

        // =========================================================
        // SYSTEM-WIDE ANNOUNCEMENTS (FULL CRUD)
        // =========================================================

        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .Where(a => a.Scope == "System")
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(announcements);
        }

        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(string Title, string Content, string TargetRole)
        {
            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(Content))
                ModelState.AddModelError(nameof(Content), "Content is required.");

            var allowedTargets = new[] { "All", "Student", "Teacher" };
            if (!allowedTargets.Contains(TargetRole))
                TargetRole = "All";

            if (!ModelState.IsValid)
                return View();

            var userId = _userManager.GetUserId(User);
            var admin = await _userManager.FindByIdAsync(userId!);
            var authorName = string.IsNullOrWhiteSpace(admin?.FullName) ? "Administrator" : admin!.FullName;

            var announcement = new Announcement
            {
                Title = Title.Trim(),
                Content = Content.Trim(),
                CreatedDate = DateTime.Now,
                AuthorUserId = userId!,
                AuthorName = authorName,
                Scope = "System",
                CourseId = null,
                TargetRole = TargetRole
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await BroadcastAnnouncementNotificationAsync(announcement);

            TempData["SuccessMessage"] = "Announcement posted successfully.";
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> EditAnnouncement(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id && a.Scope == "System");

            return announcement == null ? NotFound() : View(announcement);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(int id, string Title, string Content, string TargetRole)
        {
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id && a.Scope == "System");

            if (announcement == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(Content))
                ModelState.AddModelError(nameof(Content), "Content is required.");

            var allowedTargets = new[] { "All", "Student", "Teacher" };
            if (!allowedTargets.Contains(TargetRole))
                TargetRole = "All";

            if (!ModelState.IsValid)
                return View(announcement);

            announcement.Title = Title.Trim();
            announcement.Content = Content.Trim();
            announcement.TargetRole = TargetRole;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement updated successfully.";
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> DeleteAnnouncement(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id && a.Scope == "System");

            return announcement == null ? NotFound() : View(announcement);
        }

        [HttpPost, ActionName("DeleteAnnouncement")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncementConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement deleted successfully.";
            return RedirectToAction(nameof(Announcements));
        }

        // =========================================================
        // FEES / PAYMENTS
        // =========================================================

        public async Task<IActionResult> Fees(int? studentId, int? semesterId)
        {
            var query = _context.Fees
                .Include(f => f.Student)
                .Include(f => f.Semester)
                .Include(f => f.Payments)
                .AsNoTracking()
                .AsQueryable();

            if (studentId.HasValue) query = query.Where(f => f.StudentId == studentId.Value);
            if (semesterId.HasValue) query = query.Where(f => f.SemesterId == semesterId.Value);

            var list = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

            ViewBag.Students = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);

            ViewBag.Semesters = await BuildSemesterSelectListAsync(semesterId);

            return View(list);
        }

        public async Task<IActionResult> CreateFee()
        {
            await LoadFeeDropdownsAsync();
            return View(new Fee { DueDate = DateTime.Today.AddMonths(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFee(Fee fee)
        {
            if (!await _context.Students.AnyAsync(s => s.StudentId == fee.StudentId))
                ModelState.AddModelError(nameof(Fee.StudentId), "Select a valid student.");

            if (!await _context.Semesters.AnyAsync(s => s.SemesterId == fee.SemesterId))
                ModelState.AddModelError(nameof(Fee.SemesterId), "Select a valid semester.");

            if (await _context.Fees.AnyAsync(f => f.StudentId == fee.StudentId && f.SemesterId == fee.SemesterId))
                ModelState.AddModelError("", "A fee record already exists for this student and semester.");

            if (!ModelState.IsValid)
            {
                await LoadFeeDropdownsAsync(fee.StudentId, fee.SemesterId);
                return View(fee);
            }

            fee.CreatedDate = DateTime.Now;

            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();

            var feeStudentUserId = await GetStudentUserIdAsync(fee.StudentId);

            await AddNotificationAsync(
                feeStudentUserId,
                "New Fee Billed",
                $"A new fee of {fee.TotalAmount:N2} has been billed for your {(await _context.Semesters.FindAsync(fee.SemesterId))?.SemesterName} semester.",
                "Fee",
                Url.Action("Fees", "Student"));

            TempData["SuccessMessage"] = "Fee record created successfully.";
            return RedirectToAction(nameof(Fees));
        }

        public async Task<IActionResult> FeeDetails(int id)
        {
            var fee = await _context.Fees
                .Include(f => f.Student)
                .Include(f => f.Semester)
                .Include(f => f.Payments)
                .FirstOrDefaultAsync(f => f.FeeId == id);

            return fee == null ? NotFound() : View(fee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(
            int feeId,
            decimal AmountPaid,
            string PaymentMethod,
            string ReferenceNumber)
        {
            var fee = await _context.Fees
                .Include(f => f.Payments)
                .FirstOrDefaultAsync(f => f.FeeId == feeId);

            if (fee == null) return NotFound();

            if (AmountPaid <= 0)
            {
                TempData["DeleteError"] = "Payment amount must be greater than zero.";
                return RedirectToAction(nameof(FeeDetails), new { id = feeId });
            }

            if (string.IsNullOrWhiteSpace(ReferenceNumber))
            {
                TempData["DeleteError"] = "A reference/receipt number is required.";
                return RedirectToAction(nameof(FeeDetails), new { id = feeId });
            }

            _context.Payments.Add(new Payment
            {
                FeeId = feeId,
                AmountPaid = AmountPaid,
                PaymentDate = DateTime.Today,
                PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? "Cash" : PaymentMethod,
                ReferenceNumber = ReferenceNumber.Trim(),
                Status = "Completed"
            });

            await _context.SaveChangesAsync();

            var paymentStudentUserId = await GetStudentUserIdAsync(fee.StudentId);

            await AddNotificationAsync(
                paymentStudentUserId,
                "Payment Recorded",
                $"A payment of {AmountPaid:N2} was recorded against your fee balance (Ref: {ReferenceNumber.Trim()}).",
                "Payment",
                Url.Action("Fees", "Student"));

            TempData["SuccessMessage"] = "Payment recorded successfully.";
            return RedirectToAction(nameof(FeeDetails), new { id = feeId });
        }

        // =========================================================
        // DOCUMENTS
        // =========================================================

        public async Task<IActionResult> Documents(int? studentId)
        {
            var query = _context.Documents
                .Include(d => d.Student)
                .AsNoTracking()
                .AsQueryable();

            if (studentId.HasValue) query = query.Where(d => d.StudentId == studentId.Value);

            var list = await query.OrderByDescending(d => d.UploadedDate).ToListAsync();

            ViewBag.Students = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);

            return View(list);
        }

        public async Task<IActionResult> UploadDocument()
        {
            await LoadStudentsDropdownAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int StudentId,
            string Title,
            string DocumentType,
            IFormFile? DocumentFile)
        {
            if (!await _context.Students.AnyAsync(s => s.StudentId == StudentId))
                ModelState.AddModelError(nameof(StudentId), "Select a valid student.");

            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (DocumentFile == null)
                ModelState.AddModelError(nameof(DocumentFile), "Please choose a file to upload.");
            else
                ValidateDocumentFile(DocumentFile);

            if (!ModelState.IsValid)
            {
                await LoadStudentsDropdownAsync(StudentId);
                return View();
            }

            var fileName = await SaveDocumentFileAsync(DocumentFile!);
            var userId = _userManager.GetUserId(User);

            var document = new Document
            {
                StudentId = StudentId,
                Title = Title.Trim(),
                DocumentType = string.IsNullOrWhiteSpace(DocumentType) ? "Certificate" : DocumentType,
                FilePath = $"/uploads/documents/{fileName}",
                UploadedDate = DateTime.Now,
                UploadedByUserId = userId ?? string.Empty
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Documents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            DeleteDocumentFile(document.FilePath);

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document deleted.";
            return RedirectToAction(nameof(Documents));
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == id);
            if (document == null) return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/', '\\'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["DeleteError"] = "The requested file could not be found.";
                return RedirectToAction(nameof(Documents));
            }

            var fileName = Path.GetFileName(filePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(bytes, "application/octet-stream", fileName);
        }

        // =========================================================
        // SEND NOTIFICATIONS (BULK)
        // =========================================================

        public async Task<IActionResult> SendNotifications()
        {
            ViewBag.Courses = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotifications(
            string Target,
            int? CourseId,
            string Title,
            string Message)
        {
            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(Message))
                ModelState.AddModelError(nameof(Message), "Message is required.");

            if (Target == "Course" && !CourseId.HasValue)
                ModelState.AddModelError(nameof(CourseId), "Select a course.");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(
                    await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                    "CourseId", "CourseName", CourseId);
                return View();
            }

            List<string> userIds;

            switch (Target)
            {
                case "Students":
                    userIds = await _userManager.Users
                        .Where(u => u.StudentId != null)
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                case "Teachers":
                    userIds = await _userManager.Users
                        .Where(u => u.TeacherId != null)
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                case "Course":
                    var studentIds = await _context.Enrollments
                        .Where(e => e.CourseId == CourseId!.Value)
                        .Select(e => e.StudentId)
                        .Distinct()
                        .ToListAsync();

                    userIds = await _userManager.Users
                        .Where(u => u.StudentId != null && studentIds.Contains(u.StudentId.Value))
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                default:
                    userIds = await _userManager.Users.Select(u => u.Id).ToListAsync();
                    break;
            }

            foreach (var userId in userIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = Title.Trim(),
                    Message = Message.Trim(),
                    Type = "Announcement",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Notification sent to {userIds.Count} user(s).";
            return RedirectToAction(nameof(SendNotifications));
        }

        // =========================================================
        // LEAVE REQUESTS (GENERAL — CourseId == null)
        // =========================================================

        public async Task<IActionResult> LeaveRequests()
        {
            var requests = await _context.LeaveRequests
                .Include(l => l.Student)
                .Where(l => l.CourseId == null)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewLeaveRequest(int id, string decision, string? comment)
        {
            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id && l.CourseId == null);

            if (leaveRequest == null) return NotFound();

            if (decision != "Approved" && decision != "Rejected")
            {
                TempData["DeleteError"] = "Invalid decision.";
                return RedirectToAction(nameof(LeaveRequests));
            }

            var userId = _userManager.GetUserId(User);
            var admin = await _userManager.FindByIdAsync(userId!);

            leaveRequest.Status = decision;
            leaveRequest.ReviewedByUserId = userId;
            leaveRequest.ReviewedByName = string.IsNullOrWhiteSpace(admin?.FullName) ? "Administrator" : admin!.FullName;
            leaveRequest.ReviewedByRole = "Admin";
            leaveRequest.ReviewComment = comment;
            leaveRequest.ReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            var studentUserId = await _userManager.Users
                .Where(u => u.StudentId == leaveRequest.StudentId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(studentUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = studentUserId,
                    Title = "Leave Request Reviewed",
                    Message = $"Your leave request has been {decision.ToLower()}.",
                    Type = "Leave",
                    Link = Url.Action("LeaveRequests", "Student"),
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Leave request {decision.ToLower()}.";
            return RedirectToAction(nameof(LeaveRequests));
        }

        // =========================================================
        // REPORTS & ANALYTICS
        // =========================================================

        public async Task<IActionResult> Reports()
        {
            var results = await _context.Results.ToListAsync();
            var attendance = await _context.Attendances.ToListAsync();
            var fees = await _context.Fees.Include(f => f.Payments).ToListAsync();

            var viewModel = new AdminReportsViewModel
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalTeachers = await _context.Teachers.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                AverageGPA = results.Any(r => r.Grade != "")
                    ? Math.Round(results.Where(r => r.Grade != "").Average(r => r.GPA), 2)
                    : 0,
                OverallAttendancePercentage = attendance.Any()
                    ? Math.Round((decimal)attendance.Count(a => a.Status == "Present") / attendance.Count * 100, 2)
                    : 0,
                TotalAssignments = await _context.Assignments.CountAsync(),
                TotalSubmissions = await _context.AssignmentSubmissions.CountAsync(),
                GradedSubmissions = await _context.AssignmentSubmissions.CountAsync(s => s.Status == "Graded"),
                TotalFeesBilled = fees.Sum(f => f.TotalAmount),
                TotalFeesCollected = fees.Sum(f => f.PaidAmount),
                PendingLeaveRequests = await _context.LeaveRequests.CountAsync(l => l.Status == "Pending")
            };

            return View(viewModel);
        }

        // =========================================================
        // SYSTEM SETTINGS — SEMESTERS
        // =========================================================

        public async Task<IActionResult> Semesters()
        {
            var semesters = await _context.Semesters
                .OrderByDescending(s => s.SemesterId)
                .ToListAsync();

            return View(semesters);
        }

        public IActionResult CreateSemester()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemester(Semester semester)
        {
            if (await _context.Semesters.AnyAsync(s =>
                s.AcademicYear == semester.AcademicYear && s.SemesterName == semester.SemesterName))
            {
                ModelState.AddModelError("", "This academic year/semester combination already exists.");
            }

            if (!ModelState.IsValid)
                return View(semester);

            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Semester created successfully.";
            return RedirectToAction(nameof(Semesters));
        }

        public async Task<IActionResult> EditSemester(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters.FindAsync(id);
            return semester == null ? NotFound() : View(semester);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSemester(int id, Semester semester)
        {
            if (id != semester.SemesterId) return NotFound();

            if (await _context.Semesters.AnyAsync(s =>
                s.SemesterId != id &&
                s.AcademicYear == semester.AcademicYear &&
                s.SemesterName == semester.SemesterName))
            {
                ModelState.AddModelError("", "This academic year/semester combination already exists.");
            }

            if (!ModelState.IsValid)
                return View(semester);

            try
            {
                _context.Update(semester);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Semesters.AnyAsync(s => s.SemesterId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Semester updated successfully.";
            return RedirectToAction(nameof(Semesters));
        }

        public async Task<IActionResult> DeleteSemester(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters.FindAsync(id);
            return semester == null ? NotFound() : View(semester);
        }

        [HttpPost, ActionName("DeleteSemester")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSemesterConfirmed(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            var inUse =
                await _context.Enrollments.AnyAsync(e => e.SemesterId == id) ||
                await _context.Attendances.AnyAsync(a => a.SemesterId == id) ||
                await _context.ClassSchedules.AnyAsync(cs => cs.SemesterId == id) ||
                await _context.Fees.AnyAsync(f => f.SemesterId == id);

            if (inUse)
            {
                TempData["DeleteError"] =
                    "This semester cannot be deleted while enrollments, attendance, class schedules, or fees reference it.";
                return RedirectToAction(nameof(DeleteSemester), new { id });
            }

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Semester deleted successfully.";
            return RedirectToAction(nameof(Semesters));
        }

        // =========================================================
        // NOTIFICATIONS (admin's own notification feed)
        // =========================================================

        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
                notification.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Notifications));
        }

        // =========================================================
        // SECURITY (read-only policy/overview page; account-level
        // actions — lockout, role assignment — stay on Users & Roles
        // so that logic is never duplicated)
        // =========================================================

        public async Task<IActionResult> Security()
        {
            var viewModel = new SecurityOverviewViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                LockedOutUsers = await _userManager.Users
                    .CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow),
                AdminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count,
                TeacherCount = (await _userManager.GetUsersInRoleAsync("Teacher")).Count,
                StudentCount = (await _userManager.GetUsersInRoleAsync("Student")).Count
            };

            return View(viewModel);
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private async Task<string?> GetStudentUserIdAsync(int studentId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StudentId == studentId);

            return user?.Id;
        }

        private async Task<string?> GetTeacherUserIdAsync(int teacherId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.TeacherId == teacherId);

            return user?.Id;
        }

        private async Task AddNotificationAsync(
            string? userId,
            string title,
            string message,
            string type,
            string? link)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                CreatedDate = DateTime.Now,
                IsRead = false
            });

            await _context.SaveChangesAsync();
        }

        // Resolves the same target-user-list logic already used by
        // SendNotifications, reused here for system-wide announcements
        // so a new "All"/"Student"/"Teacher" announcement notifies the
        // right audience without duplicating the targeting logic.
        private async Task<List<string>> GetUserIdsForTargetRoleAsync(string targetRole)
        {
            return targetRole switch
            {
                "Student" => await _userManager.Users
                    .Where(u => u.StudentId != null)
                    .Select(u => u.Id)
                    .ToListAsync(),

                "Teacher" => await _userManager.Users
                    .Where(u => u.TeacherId != null)
                    .Select(u => u.Id)
                    .ToListAsync(),

                _ => await _userManager.Users
                    .Select(u => u.Id)
                    .ToListAsync()
            };
        }

        private async Task BroadcastAnnouncementNotificationAsync(Announcement announcement)
        {
            var userIds = await GetUserIdsForTargetRoleAsync(announcement.TargetRole);

            foreach (var userId in userIds)
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

                string? link = user?.StudentId != null
                    ? Url.Action("AnnouncementDetails", "Student", new { id = announcement.AnnouncementId })
                    : Url.Action("Announcements", "Teacher");

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "New Announcement",
                    Message = $"{announcement.AuthorName} posted: \"{announcement.Title}\"",
                    Type = "Announcement",
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task LoadDepartmentsAsync(int? selected = null)
        {
            ViewData["DepartmentId"] = new SelectList(
                await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync(),
                "DepartmentId", "DepartmentName", selected);
        }

        private async Task ValidateClassScheduleAsync(ClassSchedule schedule, int? excludeId = null)
        {
            if (!await _context.Courses.AnyAsync(c => c.CourseId == schedule.CourseId))
                ModelState.AddModelError(nameof(ClassSchedule.CourseId), "Select a valid course.");

            if (!await _context.Teachers.AnyAsync(t => t.TeacherId == schedule.TeacherId))
                ModelState.AddModelError(nameof(ClassSchedule.TeacherId), "Select a valid teacher.");

            if (!await _context.Semesters.AnyAsync(s => s.SemesterId == schedule.SemesterId))
                ModelState.AddModelError(nameof(ClassSchedule.SemesterId), "Select a valid semester.");

            if (schedule.EndTime <= schedule.StartTime)
                ModelState.AddModelError(nameof(ClassSchedule.EndTime), "End time must be after start time.");

            var conflict = await _context.ClassSchedules.AnyAsync(cs =>
                cs.ClassScheduleId != excludeId &&
                cs.TeacherId == schedule.TeacherId &&
                cs.SemesterId == schedule.SemesterId &&
                cs.DayOfWeek == schedule.DayOfWeek &&
                cs.StartTime < schedule.EndTime &&
                schedule.StartTime < cs.EndTime);

            if (conflict)
            {
                ModelState.AddModelError(
                    "",
                    "This teacher already has a class scheduled that overlaps this day/time in the selected semester.");
            }
        }

        private async Task LoadScheduleDropdownsAsync(
            int? courseId = null,
            int? teacherId = null,
            int? semesterId = null)
        {
            ViewData["CourseId"] = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);

            ViewData["TeacherId"] = new SelectList(
                await _context.Teachers.OrderBy(t => t.Name).ToListAsync(),
                "TeacherId", "Name", teacherId);

            ViewData["SemesterId"] = await BuildSemesterSelectListAsync(semesterId);
        }

        private async Task<SelectList> BuildSemesterSelectListAsync(int? selected = null)
        {
            var semesters = await _context.Semesters
                .OrderByDescending(s => s.SemesterId)
                .Select(s => new { s.SemesterId, Display = s.AcademicYear + " - " + s.SemesterName })
                .ToListAsync();

            return new SelectList(semesters, "SemesterId", "Display", selected);
        }

        private async Task LoadFeeDropdownsAsync(int? studentId = null, int? semesterId = null)
        {
            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);

            ViewData["SemesterId"] = await BuildSemesterSelectListAsync(semesterId);
        }

        private async Task LoadStudentsDropdownAsync(int? selected = null)
        {
            ViewBag.Students = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", selected);
        }

        private void ValidateDocumentFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);

            if (!AllowedDocumentExtensions.Contains(extension))
            {
                ModelState.AddModelError("DocumentFile", "Allowed file types: PDF, Word, JPG, PNG.");
            }

            if (file.Length <= 0 || file.Length > MaxDocumentSize)
            {
                ModelState.AddModelError("DocumentFile", "File size must be between 1 byte and 10 MB.");
            }
        }

        private async Task<string> SaveDocumentFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteDocumentFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var filePath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/', '\\'));

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
