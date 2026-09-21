using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Models;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.ViewModels;

namespace Student_Management_System.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly HashSet<string> AllowedMaterialExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".ppt", ".pptx",
                ".xls", ".xlsx", ".txt", ".zip",
                ".jpg", ".jpeg", ".png"
            };

        private const long MaxMaterialSize = 20 * 1024 * 1024; // 20 MB

        public TeacherController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // =========================================================
        // TEACHER DASHBOARD
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var userId = _userManager.GetUserId(User);

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var studentIds = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            var totalAssignments = await _context.Assignments
                .CountAsync(a => a.TeacherId == teacher.TeacherId);

            var pendingSubmissions = await _context.AssignmentSubmissions
                .CountAsync(s =>
                    s.Assignment!.TeacherId == teacher.TeacherId &&
                    s.Status != "Graded");

            var upcomingDeadlines = await _context.Assignments
                .Include(a => a.Course)
                .Where(a =>
                    a.TeacherId == teacher.TeacherId &&
                    a.DueDate >= DateTime.Now)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToListAsync();

            var attendanceRecords = await _context.Attendances
                .Where(a => courseIds.Contains(a.CourseId))
                .ToListAsync();

            decimal attendancePercentage = 0;

            if (attendanceRecords.Any())
            {
                var presentCount = attendanceRecords.Count(a => a.Status == "Present");

                attendancePercentage = Math.Round(
                    (decimal)presentCount / attendanceRecords.Count * 100,
                    2);
            }

            var recentAnnouncements = await _context.Announcements
                .Where(a =>
                    (a.Scope == "System" &&
                        (a.TargetRole == "All" || a.TargetRole == "Teacher")) ||
                    (a.Scope == "Course" &&
                        a.CourseId != null &&
                        courseIds.Contains(a.CourseId.Value)))
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();

            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var pendingLeaveRequests = await _context.LeaveRequests
                .CountAsync(l =>
                    l.CourseId != null &&
                    courseIds.Contains(l.CourseId.Value) &&
                    l.Status == "Pending");

            var viewModel = new TeacherDashboardViewModel
            {
                Teacher = teacher,
                TotalCourses = courseIds.Count,
                TotalStudents = studentIds.Count,
                TotalAssignments = totalAssignments,
                PendingSubmissions = pendingSubmissions,
                AttendancePercentage = attendancePercentage,
                PendingLeaveRequests = pendingLeaveRequests,
                UnreadNotifications = unreadNotifications,
                UpcomingDeadlines = upcomingDeadlines,
                RecentAnnouncements = recentAnnouncements,
                RecentNotifications = recentNotifications
            };

            return View(viewModel);
        }

        // Shown when a Teacher's Identity account is not yet
        // linked to a Teacher profile record (e.g. account
        // created before the Teacher record, or linkage removed).
        public IActionResult ProfileSetupRequired()
        {
            return View();
        }

        // =========================================================
        // MY PROFILE
        // =========================================================

        public async Task<IActionResult> Profile()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            return View(teacher);
        }

        // =========================================================
        // MY COURSES
        // =========================================================

        public async Task<IActionResult> MyCourses()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var schedules = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .Where(cs => cs.TeacherId == teacher.TeacherId)
                .OrderBy(cs => cs.Course!.CourseName)
                .ToListAsync();

            var courseIds = schedules
                .Select(cs => cs.CourseId)
                .Distinct()
                .ToList();

            var enrollmentPairs = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => new { e.CourseId, e.StudentId })
                .ToListAsync();

            ViewBag.StudentCounts = enrollmentPairs
                .GroupBy(e => e.CourseId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.StudentId).Distinct().Count());

            return View(schedules);
        }

        // =========================================================
        // MY STUDENTS
        // =========================================================

        public async Task<IActionResult> MyStudents(int? courseId)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            ViewBag.Courses = await _context.Courses
                .Where(c => courseIds.Contains(c.CourseId))
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.SelectedCourseId = courseId;

            var enrollmentQuery = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Where(e => courseIds.Contains(e.CourseId));

            if (courseId.HasValue)
                enrollmentQuery = enrollmentQuery.Where(e => e.CourseId == courseId.Value);

            var enrollments = await enrollmentQuery
                .OrderBy(e => e.Student!.Name)
                .ToListAsync();

            return View(enrollments);
        }

        // =========================================================
        // COURSE MATERIALS
        // =========================================================

        public async Task<IActionResult> CourseMaterials()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var materials = await _context.CourseMaterials
                .Include(m => m.Course)
                .Where(m => m.TeacherId == teacher.TeacherId)
                .OrderByDescending(m => m.UploadedDate)
                .ToListAsync();

            return View(materials);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourseMaterial()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            await LoadTeacherCoursesAsync(teacher.TeacherId);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseMaterial(
            int CourseId,
            string Title,
            string? Description,
            IFormFile? MaterialFile)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var teacherCourseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            if (!teacherCourseIds.Contains(CourseId))
            {
                ModelState.AddModelError(
                    nameof(CourseId),
                    "You may only upload materials for your own courses.");
            }

            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (MaterialFile == null)
            {
                ModelState.AddModelError(nameof(MaterialFile), "Please choose a file to upload.");
            }
            else
            {
                ValidateMaterialFile(MaterialFile);
            }

            if (!ModelState.IsValid)
            {
                await LoadTeacherCoursesAsync(teacher.TeacherId);
                return View();
            }

            var fileName = await SaveMaterialFileAsync(MaterialFile!);

            var material = new CourseMaterial
            {
                CourseId = CourseId,
                TeacherId = teacher.TeacherId,
                Title = Title.Trim(),
                Description = Description,
                FilePath = $"/uploads/materials/{fileName}",
                UploadedDate = DateTime.Now
            };

            _context.CourseMaterials.Add(material);
            await _context.SaveChangesAsync();

            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.CourseId == CourseId)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            foreach (var studentId in enrolledStudentIds)
            {
                var studentUserId = await GetStudentUserIdAsync(studentId);

                await AddNotificationAsync(
                    studentUserId,
                    "New Course Material",
                    $"\"{material.Title}\" was added to your course.",
                    "CourseMaterial",
                    Url.Action("CourseMaterials", "Student", new { courseId = material.CourseId }));
            }

            TempData["SuccessMessage"] = "Course material uploaded successfully.";

            return RedirectToAction(nameof(CourseMaterials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var material = await _context.CourseMaterials
                .FirstOrDefaultAsync(m =>
                    m.CourseMaterialId == id &&
                    m.TeacherId == teacher.TeacherId);

            if (material == null)
                return NotFound();

            DeleteMaterialFile(material.FilePath);

            _context.CourseMaterials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course material deleted.";

            return RedirectToAction(nameof(CourseMaterials));
        }

        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var material = await _context.CourseMaterials
                .FirstOrDefaultAsync(m =>
                    m.CourseMaterialId == id &&
                    m.TeacherId == teacher.TeacherId);

            if (material == null)
                return NotFound();

            var filePath = Path.Combine(
                _environment.WebRootPath,
                material.FilePath.TrimStart('/', '\\'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["DeleteError"] = "The requested file could not be found.";
                return RedirectToAction(nameof(CourseMaterials));
            }

            var fileName = Path.GetFileName(filePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(bytes, "application/octet-stream", fileName);
        }

        // =========================================================
        // COURSE ANNOUNCEMENTS
        // =========================================================

        public async Task<IActionResult> Announcements()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var announcements = await _context.Announcements
                .Include(a => a.Course)
                .Where(a =>
                    a.Scope == "Course" &&
                    a.CourseId != null &&
                    courseIds.Contains(a.CourseId.Value))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(announcements);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAnnouncement()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            await LoadTeacherCoursesAsync(teacher.TeacherId);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(
            int CourseId,
            string Title,
            string Content)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var teacherCourseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            if (!teacherCourseIds.Contains(CourseId))
            {
                ModelState.AddModelError(
                    nameof(CourseId),
                    "You may only post announcements for your own courses.");
            }

            if (string.IsNullOrWhiteSpace(Title))
                ModelState.AddModelError(nameof(Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(Content))
                ModelState.AddModelError(nameof(Content), "Content is required.");

            if (!ModelState.IsValid)
            {
                await LoadTeacherCoursesAsync(teacher.TeacherId);
                return View();
            }

            var userId = _userManager.GetUserId(User);

            var announcement = new Announcement
            {
                Title = Title.Trim(),
                Content = Content.Trim(),
                CreatedDate = DateTime.Now,
                AuthorUserId = userId!,
                AuthorName = teacher.Name ?? "Teacher",
                Scope = "Course",
                CourseId = CourseId,
                TargetRole = "Student"
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.CourseId == CourseId)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            foreach (var studentId in enrolledStudentIds)
            {
                var studentUserId = await GetStudentUserIdAsync(studentId);

                await AddNotificationAsync(
                    studentUserId,
                    "New Announcement",
                    $"{announcement.AuthorName} posted: \"{announcement.Title}\"",
                    "Announcement",
                    Url.Action("AnnouncementDetails", "Student", new { id = announcement.AnnouncementId }));
            }

            TempData["SuccessMessage"] = "Announcement posted successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // =========================================================
        // CLASS SCHEDULE (VIEW OWN)
        // =========================================================

        public async Task<IActionResult> ClassSchedule()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .Where(cs => cs.TeacherId == teacher.TeacherId)
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            return View(schedule);
        }

        // =========================================================
        // STUDENT PERFORMANCE
        // =========================================================

        public async Task<IActionResult> StudentPerformance()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var students = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.Student!)
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync();

            var assignmentIds = await _context.Assignments
                .Where(a => a.TeacherId == teacher.TeacherId)
                .Select(a => a.AssignmentId)
                .ToListAsync();

            var performanceList = new List<TeacherStudentPerformanceViewModel>();

            foreach (var student in students)
            {
                var attendanceRecords = await _context.Attendances
                    .Where(a =>
                        a.StudentId == student.StudentId &&
                        courseIds.Contains(a.CourseId))
                    .ToListAsync();

                decimal attendancePct = attendanceRecords.Any()
                    ? Math.Round(
                        (decimal)attendanceRecords.Count(a => a.Status == "Present") /
                        attendanceRecords.Count * 100,
                        2)
                    : 0;

                var results = await _context.Results
                    .Where(r =>
                        r.StudentId == student.StudentId &&
                        courseIds.Contains(r.CourseId))
                    .ToListAsync();

                decimal avgGpa = results.Any(r => r.Grade != "")
                    ? Math.Round(results.Where(r => r.Grade != "").Average(r => r.GPA), 2)
                    : 0;

                var submissionCount = await _context.AssignmentSubmissions
                    .CountAsync(s =>
                        s.StudentId == student.StudentId &&
                        assignmentIds.Contains(s.AssignmentId));

                performanceList.Add(new TeacherStudentPerformanceViewModel
                {
                    Student = student,
                    AttendancePercentage = attendancePct,
                    AverageGPA = avgGpa,
                    AssignmentsSubmitted = submissionCount,
                    TotalAssignments = assignmentIds.Count
                });
            }

            return View(performanceList);
        }

        public async Task<IActionResult> StudentPerformanceDetails(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var isMyStudent = await _context.Enrollments
                .AnyAsync(e => e.StudentId == id && courseIds.Contains(e.CourseId));

            if (!isMyStudent)
                return NotFound();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            var results = await _context.Results
                .Include(r => r.Course)
                .Where(r => r.StudentId == id && courseIds.Contains(r.CourseId))
                .ToListAsync();

            var attendance = await _context.Attendances
                .Include(a => a.Course)
                .Where(a => a.StudentId == id && courseIds.Contains(a.CourseId))
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var assignmentIds = await _context.Assignments
                .Where(a => a.TeacherId == teacher.TeacherId)
                .Select(a => a.AssignmentId)
                .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
                .Where(s => s.StudentId == id && assignmentIds.Contains(s.AssignmentId))
                .OrderByDescending(s => s.SubmittedDate)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.Results = results;
            ViewBag.Attendance = attendance;
            ViewBag.Submissions = submissions;

            return View();
        }

        // =========================================================
        // LEAVE REQUEST APPROVAL
        // =========================================================

        public async Task<IActionResult> LeaveRequests()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var requests = await _context.LeaveRequests
                .Include(l => l.Student)
                .Include(l => l.Course)
                .Where(l => l.CourseId != null && courseIds.Contains(l.CourseId.Value))
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewLeaveRequest(
            int id,
            string decision,
            string? comment)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courseIds = await GetTeacherCourseIdsAsync(teacher.TeacherId);

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l =>
                    l.LeaveRequestId == id &&
                    l.CourseId != null &&
                    courseIds.Contains(l.CourseId.Value));

            if (leaveRequest == null)
                return NotFound();

            if (decision != "Approved" && decision != "Rejected")
            {
                TempData["DeleteError"] = "Invalid decision.";
                return RedirectToAction(nameof(LeaveRequests));
            }

            var userId = _userManager.GetUserId(User);

            leaveRequest.Status = decision;
            leaveRequest.ReviewedByUserId = userId;
            leaveRequest.ReviewedByName = teacher.Name;
            leaveRequest.ReviewedByRole = "Teacher";
            leaveRequest.ReviewComment = comment;
            leaveRequest.ReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            var studentUserId = await GetStudentUserIdAsync(leaveRequest.StudentId);

            await AddNotificationAsync(
                studentUserId,
                "Leave Request Reviewed",
                $"Your leave request has been {decision.ToLower()}.",
                "Leave",
                Url.Action("LeaveRequests", "Student"));

            TempData["SuccessMessage"] = $"Leave request {decision.ToLower()}.";

            return RedirectToAction(nameof(LeaveRequests));
        }

        // =========================================================
        // CONTACT STUDENTS
        // =========================================================

        public async Task<IActionResult> Messages(int? id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            if (id.HasValue)
                return RedirectToAction(nameof(MessageThread), new { id = id.Value });

            var threads = await _context.ContactMessages
                .Include(m => m.Student)
                .Include(m => m.Course)
                .Include(m => m.Replies)
                .Where(m =>
                    m.TeacherId == teacher.TeacherId &&
                    m.ParentMessageId == null)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(threads);
        }

        public async Task<IActionResult> MessageThread(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var thread = await _context.ContactMessages
                .Include(m => m.Student)
                .Include(m => m.Course)
                .Include(m => m.Replies)
                .FirstOrDefaultAsync(m =>
                    m.ContactMessageId == id &&
                    m.TeacherId == teacher.TeacherId &&
                    m.ParentMessageId == null);

            if (thread == null)
                return NotFound();

            return View(thread);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string ReplyText)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var originalMessage = await _context.ContactMessages
                .FirstOrDefaultAsync(m =>
                    m.ContactMessageId == id &&
                    m.TeacherId == teacher.TeacherId &&
                    m.ParentMessageId == null);

            if (originalMessage == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(ReplyText))
            {
                TempData["DeleteError"] = "Please enter a reply.";
                return RedirectToAction(nameof(MessageThread), new { id });
            }

            var reply = new ContactMessage
            {
                StudentId = originalMessage.StudentId,
                TeacherId = originalMessage.TeacherId,
                CourseId = originalMessage.CourseId,
                Subject = $"Re: {originalMessage.Subject}",
                MessageText = ReplyText,
                SenderRole = "Teacher",
                Status = "Replied",
                SentDate = DateTime.Now,
                ParentMessageId = originalMessage.ContactMessageId
            };

            _context.ContactMessages.Add(reply);

            originalMessage.Status = "Replied";

            await _context.SaveChangesAsync();

            var studentUserId = await GetStudentUserIdAsync(originalMessage.StudentId);

            await AddNotificationAsync(
                studentUserId,
                "New Reply",
                $"{teacher.Name} replied to your message: \"{originalMessage.Subject}\"",
                "Message",
                Url.Action("MessageThread", "Student", new { id = originalMessage.ContactMessageId }));

            TempData["SuccessMessage"] = "Reply sent.";

            return RedirectToAction(nameof(MessageThread), new { id });
        }

        // =========================================================
        // REPORTS
        // =========================================================

        public async Task<IActionResult> Reports()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(ProfileSetupRequired));

            var courses = await _context.ClassSchedules
                .Include(cs => cs.Course)
                .Where(cs => cs.TeacherId == teacher.TeacherId)
                .Select(cs => cs.Course!)
                .Distinct()
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var reportRows = new List<TeacherCourseReportViewModel>();

            foreach (var course in courses)
            {
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.CourseId == course.CourseId)
                    .ToListAsync();

                decimal attendancePct = attendanceRecords.Any()
                    ? Math.Round(
                        (decimal)attendanceRecords.Count(a => a.Status == "Present") /
                        attendanceRecords.Count * 100,
                        2)
                    : 0;

                var results = await _context.Results
                    .Where(r => r.CourseId == course.CourseId)
                    .ToListAsync();

                decimal avgGpa = results.Any(r => r.Grade != "")
                    ? Math.Round(results.Where(r => r.Grade != "").Average(r => r.GPA), 2)
                    : 0;

                var totalAssignments = await _context.Assignments
                    .CountAsync(a => a.CourseId == course.CourseId && a.TeacherId == teacher.TeacherId);

                var totalSubmissions = await _context.AssignmentSubmissions
                    .CountAsync(s =>
                        s.Assignment!.CourseId == course.CourseId &&
                        s.Assignment!.TeacherId == teacher.TeacherId);

                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.CourseId == course.CourseId);

                reportRows.Add(new TeacherCourseReportViewModel
                {
                    Course = course,
                    EnrolledStudents = enrolledCount,
                    AttendancePercentage = attendancePct,
                    AverageGPA = avgGpa,
                    TotalAssignments = totalAssignments,
                    TotalSubmissions = totalSubmissions
                });
            }

            return View(reportRows);
        }

        // =========================================================
        // NOTIFICATIONS (teacher's own notification feed)
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
        // HELPERS
        // =========================================================

        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var applicationUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (applicationUser == null || !applicationUser.TeacherId.HasValue)
                return null;

            return await _context.Teachers
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.TeacherId == applicationUser.TeacherId.Value);
        }

        private async Task<List<int>> GetTeacherCourseIdsAsync(int teacherId)
        {
            return await _context.ClassSchedules
                .Where(cs => cs.TeacherId == teacherId)
                .Select(cs => cs.CourseId)
                .Distinct()
                .ToListAsync();
        }

        private async Task LoadTeacherCoursesAsync(int teacherId)
        {
            var courseIds = await GetTeacherCourseIdsAsync(teacherId);

            ViewBag.Courses = await _context.Courses
                .Where(c => courseIds.Contains(c.CourseId))
                .OrderBy(c => c.CourseName)
                .ToListAsync();
        }

        private async Task<string?> GetStudentUserIdAsync(int studentId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StudentId == studentId);

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

        private void ValidateMaterialFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);

            if (!AllowedMaterialExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "MaterialFile",
                    "Allowed file types: PDF, Word, PowerPoint, Excel, TXT, ZIP, JPG, PNG.");
            }

            if (file.Length <= 0 || file.Length > MaxMaterialSize)
            {
                ModelState.AddModelError(
                    "MaterialFile",
                    "File size must be between 1 byte and 20 MB.");
            }
        }

        private async Task<string> SaveMaterialFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "materials");

            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);

            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteMaterialFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var filePath = Path.Combine(
                _environment.WebRootPath,
                relativePath.TrimStart('/', '\\'));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
