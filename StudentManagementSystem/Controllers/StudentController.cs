using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using Student_Management_System.Models;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly HashSet<string> AllowedPhotoExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private const long MaxPhotoSize = 5 * 1024 * 1024;

        public StudentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // =========================================================
        // STUDENT DASHBOARD
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Dashboard()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var userId = _userManager.GetUserId(User);

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            // ---------------------------------------------------------
            // GPA
            // ---------------------------------------------------------

            var resultsWithGrades = await _context.Results
                .Where(r =>
                    r.StudentId == student.StudentId &&
                    r.Grade != "")
                .ToListAsync();

            decimal overallGpa = resultsWithGrades.Any()
                ? Math.Round(resultsWithGrades.Average(r => r.GPA), 2)
                : 0;

            // ---------------------------------------------------------
            // ATTENDANCE PERCENTAGE
            // ---------------------------------------------------------

            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == student.StudentId)
                .ToListAsync();

            decimal attendancePercentage = 0;

            if (attendanceRecords.Any())
            {
                var presentCount = attendanceRecords
                    .Count(a => a.Status == "Present");

                attendancePercentage = Math.Round(
                    (decimal)presentCount / attendanceRecords.Count * 100,
                    2);
            }

            // ---------------------------------------------------------
            // ASSIGNMENTS (pending + upcoming deadlines)
            // ---------------------------------------------------------

            var submittedAssignmentIds = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == student.StudentId)
                .Select(s => s.AssignmentId)
                .ToListAsync();

            var courseAssignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => enrolledCourseIds.Contains(a.CourseId))
                .ToListAsync();

            var pendingAssignments = courseAssignments
                .Where(a => !submittedAssignmentIds.Contains(a.AssignmentId))
                .ToList();

            var upcomingDeadlines = pendingAssignments
                .Where(a => a.DueDate >= DateTime.Now)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToList();

            // ---------------------------------------------------------
            // ANNOUNCEMENTS
            // ---------------------------------------------------------

            var recentAnnouncements = await _context.Announcements
                .Where(a =>
                    (a.Scope == "System" &&
                        (a.TargetRole == "All" || a.TargetRole == "Student")) ||
                    (a.Scope == "Course" &&
                        a.CourseId != null &&
                        enrolledCourseIds.Contains(a.CourseId.Value)))
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();

            // ---------------------------------------------------------
            // NOTIFICATIONS
            // ---------------------------------------------------------

            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            // ---------------------------------------------------------
            // LEAVE REQUESTS
            // ---------------------------------------------------------

            var pendingLeaveRequests = await _context.LeaveRequests
                .CountAsync(l =>
                    l.StudentId == student.StudentId &&
                    l.Status == "Pending");

            var viewModel = new StudentDashboardViewModel
            {
                Student = student,
                TotalCourses = enrolledCourseIds.Count,
                OverallGPA = overallGpa,
                AttendancePercentage = attendancePercentage,
                PendingAssignments = pendingAssignments.Count,
                PendingLeaveRequests = pendingLeaveRequests,
                UnreadNotifications = unreadNotifications,
                UpcomingDeadlines = upcomingDeadlines,
                RecentAnnouncements = recentAnnouncements,
                RecentNotifications = recentNotifications
            };

            return View(viewModel);
        }

        // =========================================================
        // STUDENT PROFILE
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Profile()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            return View(student);
        }

        [Authorize(Roles = "Student")]
        public IActionResult ProfileSetupRequired()
        {
            return View();
        }

        // =========================================================
        // STUDENT COURSES
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Courses()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courses = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Semester)
                .Where(e => e.StudentId == student.StudentId)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            return View(courses);
        }

        // =========================================================
        // COURSE MATERIALS (view-only, for enrolled courses)
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CourseMaterials(int courseId)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == student.StudentId && e.CourseId == courseId);

            if (!isEnrolled)
                return Forbid();

            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
                return NotFound();

            var materials = await _context.CourseMaterials
                .Include(m => m.Teacher)
                .Where(m => m.CourseId == courseId)
                .OrderByDescending(m => m.UploadedDate)
                .ToListAsync();

            ViewBag.Course = course;

            return View(materials);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var material = await _context.CourseMaterials
                .FirstOrDefaultAsync(m => m.CourseMaterialId == id);

            if (material == null)
                return NotFound();

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == student.StudentId && e.CourseId == material.CourseId);

            if (!isEnrolled)
                return Forbid();

            var filePath = Path.Combine(
                _environment.WebRootPath,
                material.FilePath.TrimStart('/', '\\'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "The requested file could not be found.";
                return RedirectToAction(nameof(CourseMaterials), new { courseId = material.CourseId });
            }

            var fileName = Path.GetFileName(filePath);

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(bytes, "application/octet-stream", fileName);
        }

        // =========================================================
        // STUDENT RESULTS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Results()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var results = await _context.Results
                .Include(r => r.Course)
                .Where(r => r.StudentId == student.StudentId)
                .OrderByDescending(r => r.ResultDate)
                .ToListAsync();

            return View(results);
        }

        // =========================================================
        // STUDENT ATTENDANCE
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Attendance()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var attendance = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Semester)
                .Where(a => a.StudentId == student.StudentId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            return View(attendance);
        }

        // =========================================================
        // STUDENT ACADEMIC PROGRESS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AcademicProgress()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            // ---------------------------------------------------------
            // GET STUDENT ENROLLMENTS
            // ---------------------------------------------------------

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Semester)
                .Where(e => e.StudentId == student.StudentId)
                .OrderByDescending(e => e.SemesterId)
                .ThenBy(e => e.Course!.CourseName)
                .ToListAsync();

            // ---------------------------------------------------------
            // GET STUDENT RESULTS
            // ---------------------------------------------------------

            var results = await _context.Results
                .Include(r => r.Course)
                .Where(r => r.StudentId == student.StudentId)
                .ToListAsync();

            // ---------------------------------------------------------
            // CREATE COURSE PROGRESS LIST
            // ---------------------------------------------------------

            var courseProgress = new List<AcademicProgressItemViewModel>();

            foreach (var enrollment in enrollments)
            {
                if (enrollment.Course == null)
                    continue;

                var result = results.FirstOrDefault(r =>
                    r.CourseId == enrollment.CourseId);

                var item = new AcademicProgressItemViewModel
                {
                    CourseName = enrollment.Course.CourseName ?? "N/A",

                    CourseCode = enrollment.Course.CourseCode ?? "N/A",

                    CreditHours = enrollment.Course.CreditHours,

                    SemesterName =
                        enrollment.Semester?.SemesterName ?? "N/A",

                    AcademicYear =
                        enrollment.Semester?.AcademicYear ?? "N/A",

                    TotalMarks = result?.TotalMarks ?? 0,

                    Grade = result?.Grade ?? "Not Available",

                    GPA = result?.GPA ?? 0,

                    HasResult = result != null
                };

                courseProgress.Add(item);
            }

            // ---------------------------------------------------------
            // CALCULATE OVERALL GPA
            // ---------------------------------------------------------

            var resultsWithGrades = results
                .Where(r => !string.IsNullOrWhiteSpace(r.Grade))
                .ToList();

            decimal overallGPA = 0;

            if (resultsWithGrades.Any())
            {
                overallGPA = resultsWithGrades
                    .Average(r => r.GPA);
            }

            // ---------------------------------------------------------
            // CALCULATE AVERAGE MARKS
            // ---------------------------------------------------------

            decimal averageMarks = 0;

            if (results.Any())
            {
                averageMarks = results
                    .Average(r => r.TotalMarks);
            }

            // ---------------------------------------------------------
            // CALCULATE PASSED COURSES
            // ---------------------------------------------------------

            int passedCourses = results.Count(r =>
                r.GPA > 0);

            // ---------------------------------------------------------
            // CREATE VIEW MODEL
            // ---------------------------------------------------------

            var viewModel = new AcademicProgressViewModel
            {
                Student = student,

                Courses = courseProgress,

                OverallGPA = Math.Round(
                    overallGPA,
                    2),

                AverageMarks = Math.Round(
                    averageMarks,
                    2),

                TotalCourses = enrollments.Count,

                PassedCourses = passedCourses
            };

            return View(viewModel);
        }


        // =========================================================
        // ADMIN / TEACHER STUDENT LIST
        // =========================================================

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Index(
            string? searchString,
            int? departmentId,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 5;

            page = Math.Max(page, 1);

            var students = _context.Students
                .Include(s => s.Department)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                students = students.Where(s =>
                    (s.Name ?? "").Contains(searchString) ||
                    (s.Email ?? "").Contains(searchString) ||
                    (s.Phone ?? "").Contains(searchString) ||
                    (s.RegistrationNumber ?? "").Contains(searchString));
            }

            if (departmentId.HasValue)
            {
                students = students.Where(
                    s => s.DepartmentId == departmentId.Value);
            }

            students = sortOrder switch
            {
                "name_desc" =>
                    students.OrderByDescending(s => s.Name),

                "email" =>
                    students.OrderBy(s => s.Email),

                "email_desc" =>
                    students.OrderByDescending(s => s.Email),

                "registration" =>
                    students.OrderBy(s => s.RegistrationNumber),

                "registration_desc" =>
                    students.OrderByDescending(s => s.RegistrationNumber),

                _ =>
                    students.OrderBy(s => s.Name)
            };

            var totalStudents = await students.CountAsync();

            var totalPages =
                (int)Math.Ceiling(
                    totalStudents / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var studentList = await students
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentDepartment"] = departmentId;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["NameSort"] =
                sortOrder == "name_desc"
                    ? ""
                    : "name_desc";

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.Departments = new SelectList(
                await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync(),
                "DepartmentId",
                "DepartmentName",
                departmentId);

            return View(studentList);
        }

        // =========================================================
        // STUDENT DETAILS
        // =========================================================

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Semester)
                .Include(s => s.Attendances)
                    .ThenInclude(a => a.Course)
                .Include(s => s.Results)
                    .ThenInclude(r => r.Course)
                .FirstOrDefaultAsync(
                    s => s.StudentId == id);

            return student == null
                ? NotFound()
                : View(student);
        }

        // =========================================================
        // CREATE STUDENT - GET
        // =========================================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadDepartmentsAsync();

            return View();
        }

        // =========================================================
        // CREATE STUDENT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            Student student,
            string password)
        {
            ModelState.Remove(nameof(Student.PhotoFile));

            // -----------------------------------------------------
            // PASSWORD VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError(
                    "password",
                    "Password must contain at least 6 characters.");
            }

            // -----------------------------------------------------
            // STUDENT EMAIL DUPLICATE CHECK
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                var studentEmailExists =
                    await _context.Students
                        .AnyAsync(s => s.Email == student.Email);

                if (studentEmailExists)
                {
                    ModelState.AddModelError(
                        nameof(Student.Email),
                        "Email already exists.");
                }

                var identityEmailExists =
                    await _userManager.FindByEmailAsync(student.Email);

                if (identityEmailExists != null)
                {
                    ModelState.AddModelError(
                        nameof(Student.Email),
                        "This email already has a login account.");
                }
            }

            // -----------------------------------------------------
            // DUPLICATE NAME
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(student.Name) &&
                await _context.Students
                    .AnyAsync(s => s.Name == student.Name))
            {
                ModelState.AddModelError(
                    nameof(Student.Name),
                    "A student with this name already exists.");
            }

            // -----------------------------------------------------
            // PHOTO
            // -----------------------------------------------------

            if (student.PhotoFile != null)
                ValidatePhoto(student.PhotoFile);

            // -----------------------------------------------------
            // RETURN FORM IF INVALID
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(student.DepartmentId);

                return View(student);
            }

            // -----------------------------------------------------
            // GENERATE REGISTRATION NUMBER
            // -----------------------------------------------------

            student.RegistrationNumber =
                await GenerateRegistrationNumberAsync();

            // -----------------------------------------------------
            // SAVE PHOTO
            // -----------------------------------------------------

            student.PhotoPath =
                await SavePhotoAsync(student.PhotoFile);

            // -----------------------------------------------------
            // CREATE STUDENT DATABASE RECORD
            // -----------------------------------------------------

            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // CREATE IDENTITY USER
            // -----------------------------------------------------

            var applicationUser = new ApplicationUser
            {
                UserName = student.Email,
                Email = student.Email,
                FullName = student.Name ?? string.Empty,
                EmailConfirmed = true,

                // Link Identity account to Student
                StudentId = student.StudentId
            };

            var identityResult =
                await _userManager.CreateAsync(
                    applicationUser,
                    password);

            // -----------------------------------------------------
            // IF IDENTITY ACCOUNT CREATION FAILED
            // -----------------------------------------------------

            if (!identityResult.Succeeded)
            {
                // Remove the Student record because its login
                // could not be created.

                _context.Students.Remove(student);

                await _context.SaveChangesAsync();

                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError(
                        "password",
                        error.Description);
                }

                await LoadDepartmentsAsync(
                    student.DepartmentId);

                return View(student);
            }

            // -----------------------------------------------------
            // ASSIGN STUDENT ROLE
            // -----------------------------------------------------

            var roleResult =
                await _userManager.AddToRoleAsync(
                    applicationUser,
                    "Student");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        $"Student role error: {error.Description}");
                }

                await LoadDepartmentsAsync(
                    student.DepartmentId);

                return View(student);
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                $"Student created successfully. " +
                $"Registration Number: {student.RegistrationNumber}. " +
                $"Login Email: {student.Email}";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT STUDENT
        // =========================================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var student =
                await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            await LoadDepartmentsAsync(
                student.DepartmentId);

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            Student student)
        {
            if (id != student.StudentId)
                return NotFound();

            ModelState.Remove(nameof(Student.PhotoFile));

            if (await _context.Students.AnyAsync(s =>
                s.Email == student.Email &&
                s.StudentId != student.StudentId))
            {
                ModelState.AddModelError(
                    nameof(Student.Email),
                    "Email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(student.Name) &&
                await _context.Students.AnyAsync(s =>
                    s.Name == student.Name &&
                    s.StudentId != student.StudentId))
            {
                ModelState.AddModelError(
                    nameof(Student.Name),
                    "A student with this name already exists.");
            }

            if (student.PhotoFile != null)
                ValidatePhoto(student.PhotoFile);

            var existing =
                await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        s => s.StudentId == id);

            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                student.PhotoPath =
                    existing.PhotoPath;

                await LoadDepartmentsAsync(
                    student.DepartmentId);

                return View(student);
            }

            student.RegistrationNumber =
                existing.RegistrationNumber;

            student.PhotoPath =
                existing.PhotoPath;

            if (student.PhotoFile != null)
            {
                var newPhoto =
                    await SavePhotoAsync(
                        student.PhotoFile);

                if (newPhoto != null)
                {
                    DeletePhoto(existing.PhotoPath);

                    student.PhotoPath = newPhoto;
                }
            }

            _context.Update(student);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // UPDATE LINKED IDENTITY ACCOUNT
            // -----------------------------------------------------

            var identityUser =
                await _userManager.Users
                    .FirstOrDefaultAsync(
                        u => u.StudentId == student.StudentId);

            if (identityUser != null)
            {
                identityUser.FullName =
                    student.Name ?? string.Empty;

                identityUser.Email =
                    student.Email;

                identityUser.UserName =
                    student.Email;

                await _userManager.UpdateAsync(
                    identityUser);
            }

            TempData["SuccessMessage"] =
                "Student updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE STUDENT
        // =========================================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var student =
                await _context.Students
                    .Include(s => s.Department)
                    .FirstOrDefaultAsync(
                        s => s.StudentId == id);

            return student == null
                ? NotFound()
                : View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var student =
                await _context.Students
                    .Include(s => s.Enrollments)
                    .Include(s => s.Attendances)
                    .Include(s => s.Results)
                    .FirstOrDefaultAsync(
                        s => s.StudentId == id);

            if (student == null)
                return NotFound();

            if ((student.Enrollments?.Count ?? 0) > 0 ||
                (student.Attendances?.Count ?? 0) > 0 ||
                (student.Results?.Count ?? 0) > 0)
            {
                TempData["DeleteError"] =
                    "This student cannot be deleted while " +
                    "enrollments, attendance records, or " +
                    "results exist.";

                return RedirectToAction(
                    nameof(Delete),
                    new { id });
            }

            // -----------------------------------------------------
            // DELETE LINKED IDENTITY ACCOUNT
            // -----------------------------------------------------

            var identityUser =
                await _userManager.Users
                    .FirstOrDefaultAsync(
                        u => u.StudentId == student.StudentId);

            if (identityUser != null)
            {
                await _userManager.DeleteAsync(
                    identityUser);
            }

            // -----------------------------------------------------
            // DELETE PHOTO
            // -----------------------------------------------------

            DeletePhoto(student.PhotoPath);

            // -----------------------------------------------------
            // DELETE STUDENT
            // -----------------------------------------------------

            _context.Students.Remove(student);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Student deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ANNOUNCEMENTS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Announcements()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var announcements = await _context.Announcements
                .Include(a => a.Course)
                .Where(a =>
                    (a.Scope == "System" &&
                        (a.TargetRole == "All" || a.TargetRole == "Student")) ||
                    (a.Scope == "Course" &&
                        a.CourseId != null &&
                        enrolledCourseIds.Contains(a.CourseId.Value)))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(announcements);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AnnouncementDetails(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var announcement = await _context.Announcements
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.AnnouncementId == id &&
                    ((a.Scope == "System" &&
                        (a.TargetRole == "All" || a.TargetRole == "Student")) ||
                    (a.Scope == "Course" &&
                        a.CourseId != null &&
                        enrolledCourseIds.Contains(a.CourseId.Value))));

            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        // =========================================================
        // CLASS SCHEDULE (VIEW-ONLY)
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ClassSchedule()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => new { e.CourseId, e.SemesterId })
                .Distinct()
                .ToListAsync();

            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var semesterIds = enrollments.Select(e => e.SemesterId).ToList();

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Semester)
                .Where(cs =>
                    courseIds.Contains(cs.CourseId) &&
                    semesterIds.Contains(cs.SemesterId))
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            return View(schedule);
        }

        // =========================================================
        // FEES / PAYMENTS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Fees()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var fees = await _context.Fees
                .Include(f => f.Semester)
                .Include(f => f.Payments)
                .Where(f => f.StudentId == student.StudentId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            return View(fees);
        }

        // =========================================================
        // DOCUMENTS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Documents()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var documents = await _context.Documents
                .Where(d => d.StudentId == student.StudentId)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();

            return View(documents);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var document = await _context.Documents
                .FirstOrDefaultAsync(d =>
                    d.DocumentId == id &&
                    d.StudentId == student.StudentId);

            if (document == null)
                return NotFound();

            var filePath = Path.Combine(
                _environment.WebRootPath,
                document.FilePath.TrimStart('/', '\\'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "The requested file could not be found.";
                return RedirectToAction(nameof(Documents));
            }

            var fileName = Path.GetFileName(filePath);
            var contentType = "application/octet-stream";

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(bytes, contentType, fileName);
        }

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        [Authorize(Roles = "Student")]
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
        // LEAVE REQUESTS
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> LeaveRequests()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var requests = await _context.LeaveRequests
                .Include(l => l.Course)
                .Where(l => l.StudentId == student.StudentId)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> CreateLeaveRequest()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            await LoadStudentCoursesAsync(student.StudentId);

            return View();
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeaveRequest(LeaveRequest model)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            ModelState.Remove(nameof(LeaveRequest.StudentId));
            ModelState.Remove(nameof(LeaveRequest.Status));

            if (!ModelState.IsValid)
            {
                await LoadStudentCoursesAsync(student.StudentId);
                return View(model);
            }

            var leaveRequest = new LeaveRequest
            {
                StudentId = student.StudentId,
                CourseId = model.CourseId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request submitted successfully.";

            return RedirectToAction(nameof(LeaveRequests));
        }

        // =========================================================
        // CONTACT TEACHER
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ContactTeacher()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var messages = await _context.ContactMessages
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .Include(m => m.Replies)
                .Where(m =>
                    m.StudentId == student.StudentId &&
                    m.ParentMessageId == null)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(messages);
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> SendMessage()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            await LoadCourseTeachersAsync(student.StudentId);

            return View();
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            string TeacherCourse,
            string Subject,
            string MessageText)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var parts = (TeacherCourse ?? string.Empty).Split('|');

            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var TeacherId) ||
                !int.TryParse(parts[1], out var CourseId) ||
                string.IsNullOrWhiteSpace(Subject) ||
                string.IsNullOrWhiteSpace(MessageText))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select a teacher and enter a subject and message.");

                await LoadCourseTeachersAsync(student.StudentId);

                return View();
            }

            var message = new ContactMessage
            {
                StudentId = student.StudentId,
                TeacherId = TeacherId,
                CourseId = CourseId,
                Subject = Subject,
                MessageText = MessageText,
                SenderRole = "Student",
                Status = "New",
                SentDate = DateTime.Now
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            var teacherUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.TeacherId == TeacherId);

            if (teacherUser != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = teacherUser.Id,
                    Title = "New Message",
                    Message = $"{student.Name} sent you a message: \"{Subject}\"",
                    Type = "Message",
                    Link = Url.Action("Messages", "Teacher", new { id = message.ContactMessageId }),
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Message sent successfully.";

            return RedirectToAction(nameof(ContactTeacher));
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MessageThread(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var thread = await _context.ContactMessages
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .Include(m => m.Replies)
                .FirstOrDefaultAsync(m =>
                    m.ContactMessageId == id &&
                    m.StudentId == student.StudentId &&
                    m.ParentMessageId == null);

            if (thread == null)
                return NotFound();

            return View(thread);
        }

        // =========================================================
        // HELPERS - STUDENT COURSES / TEACHERS FOR DROPDOWNS
        // =========================================================

        private async Task LoadStudentCoursesAsync(int studentId)
        {
            var courseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();

            ViewBag.Courses = await _context.Courses
                .Where(c => courseIds.Contains(c.CourseId))
                .OrderBy(c => c.CourseName)
                .ToListAsync();
        }

        private async Task LoadCourseTeachersAsync(int studentId)
        {
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var schedules = await _context.ClassSchedules
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Course)
                .Where(cs => enrolledCourseIds.Contains(cs.CourseId))
                .ToListAsync();

            ViewBag.CourseTeachers = schedules
                .Where(cs => cs.Teacher != null && cs.Course != null)
                .GroupBy(cs => new { cs.TeacherId, cs.CourseId })
                .Select(g => g.First())
                .Select(cs => new SelectListItem
                {
                    Value = cs.TeacherId + "|" + cs.CourseId,
                    Text = $"{cs.Teacher!.Name} — {cs.Course!.CourseName}"
                })
                .ToList();
        }

        // =========================================================
        // GET CURRENT LOGGED-IN STUDENT
        // =========================================================

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var applicationUser =
                await _userManager.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userId);

            if (applicationUser == null ||
                !applicationUser.StudentId.HasValue)
            {
                return null;
            }

            return await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(
                    s => s.StudentId ==
                         applicationUser.StudentId.Value);
        }

        // =========================================================
        // LOAD DEPARTMENTS
        // =========================================================

        private async Task LoadDepartmentsAsync(
            int? selected = null)
        {
            ViewData["DepartmentId"] =
                new SelectList(
                    await _context.Departments
                        .OrderBy(d => d.DepartmentName)
                        .ToListAsync(),
                    "DepartmentId",
                    "DepartmentName",
                    selected);
        }

        // =========================================================
        // PHOTO VALIDATION
        // =========================================================

        private void ValidatePhoto(IFormFile file)
        {
            var extension =
                Path.GetExtension(file.FileName);

            if (!AllowedPhotoExtensions.Contains(
                extension))
            {
                ModelState.AddModelError(
                    nameof(Student.PhotoFile),
                    "Only JPG, JPEG, PNG, and WEBP " +
                    "images are allowed.");
            }

            if (file.Length <= 0 ||
                file.Length > MaxPhotoSize)
            {
                ModelState.AddModelError(
                    nameof(Student.PhotoFile),
                    "Photo size must be between 1 byte " +
                    "and 5 MB.");
            }
        }

        // =========================================================
        // SAVE PHOTO
        // =========================================================

        private async Task<string?> SavePhotoAsync(
            IFormFile? file)
        {
            if (file == null)
                return null;

            var extension =
                Path.GetExtension(
                    file.FileName)
                .ToLowerInvariant();

            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "students");

            Directory.CreateDirectory(folder);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    folder,
                    fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew);

            await file.CopyToAsync(stream);

            return fileName;
        }

        // =========================================================
        // DELETE PHOTO
        // =========================================================

        private void DeletePhoto(
            string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var path =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "students",
                    fileName);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        // =========================================================
        // GENERATE REGISTRATION NUMBER
        // =========================================================

        private async Task<string>
            GenerateRegistrationNumberAsync()
        {
            var year =
                DateTime.Today.Year;

            var prefix =
                $"ST{year}";

            var numbers =
                await _context.Students
                    .Where(s =>
                        s.RegistrationNumber != null &&
                        s.RegistrationNumber
                            .StartsWith(prefix))
                    .Select(s =>
                        s.RegistrationNumber!)
                    .ToListAsync();

            var max =
                numbers
                    .Select(n =>
                        int.TryParse(
                            n.Substring(prefix.Length),
                            out var value)
                            ? value
                            : 0)
                    .DefaultIfEmpty(0)
                    .Max();

            return
                $"{prefix}{max + 1:000}";
        }
    }
}