using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Controllers;
using Student_Management_System.Models;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // TEACHER - ASSIGNMENT LIST
        // =========================================================

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => a.TeacherId == teacher.TeacherId)
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            return View(assignments);
        }

        // =========================================================
        // TEACHER - CREATE
        // =========================================================

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Assignment assignment)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            ModelState.Remove(nameof(Assignment.TeacherId));

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(assignment);
            }

            assignment.TeacherId = teacher.TeacherId;
            assignment.CreatedDate = DateTime.Now;

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            await NotifyStudentsOfNewAssignmentAsync(assignment);

            TempData["Success"] = "Assignment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TEACHER - DETAILS
        // =========================================================

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        // =========================================================
        // TEACHER - EDIT
        // =========================================================

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (assignment == null)
                return NotFound();

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(assignment);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Assignment assignment)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            if (id != assignment.AssignmentId)
                return NotFound();

            var existingAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (existingAssignment == null)
                return NotFound();

            ModelState.Remove(nameof(Assignment.TeacherId));

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(assignment);
            }

            existingAssignment.Title = assignment.Title;
            existingAssignment.Description = assignment.Description;
            existingAssignment.CourseId = assignment.CourseId;
            existingAssignment.DueDate = assignment.DueDate;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TEACHER - DELETE
        // =========================================================

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (assignment == null)
                return NotFound();

            _context.Assignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TEACHER - VIEW SUBMISSIONS
        // =========================================================

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Submissions(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == id &&
                    a.TeacherId == teacher.TeacherId);

            if (assignment == null)
                return NotFound();

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == id)
                .OrderByDescending(s => s.SubmittedDate)
                .ToListAsync();

            ViewBag.Assignment = assignment;

            return View(submissions);
        }

        // =========================================================
        // TEACHER - GRADE A SUBMISSION
        // =========================================================

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
                .FirstOrDefaultAsync(s =>
                    s.SubmissionId == id &&
                    s.Assignment!.TeacherId == teacher.TeacherId);

            if (submission == null)
                return NotFound();

            return View(submission);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(
            int id,
            decimal? Marks,
            string? Feedback)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
                return RedirectToAction(nameof(TeacherController.ProfileSetupRequired), "Teacher");

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s =>
                    s.SubmissionId == id &&
                    s.Assignment!.TeacherId == teacher.TeacherId);

            if (submission == null)
                return NotFound();

            if (Marks.HasValue && (Marks.Value < 0 || Marks.Value > 100))
            {
                ModelState.AddModelError(nameof(Marks), "Marks must be between 0 and 100.");
                return View(submission);
            }

            submission.Marks = Marks;
            submission.Feedback = Feedback;
            submission.Status = "Graded";
            submission.GradedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                await GetStudentUserIdAsync(submission.StudentId),
                "Assignment Graded",
                $"Your submission for \"{submission.Assignment?.Title}\" has been graded.",
                "Assignment",
                Url.Action(nameof(MySubmission), "Assignment", new { id = submission.AssignmentId }));

            TempData["Success"] = "Submission graded successfully.";

            return RedirectToAction(nameof(Submissions), new { id = submission.AssignmentId });
        }

        // =========================================================
        // STUDENT - ASSIGNMENT LIST
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentAssignments()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(StudentController.ProfileSetupRequired), "Student");

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => enrolledCourseIds.Contains(a.CourseId))
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            var submittedAssignmentIds = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == student.StudentId)
                .Select(s => s.AssignmentId)
                .ToListAsync();

            ViewBag.SubmittedAssignmentIds = submittedAssignmentIds;

            return View(assignments);
        }

        // =========================================================
        // STUDENT - ASSIGNMENT DETAILS
        // =========================================================

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(StudentController.ProfileSetupRequired), "Student");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            var submission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == id &&
                    s.StudentId == student.StudentId);

            ViewBag.Submission = submission;

            return View(assignment);
        }

        // =========================================================
        // STUDENT - SUBMIT ASSIGNMENT
        // =========================================================

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Submit(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(StudentController.ProfileSetupRequired), "Student");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            var existingSubmission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == id &&
                    s.StudentId == student.StudentId);

            if (existingSubmission != null)
            {
                TempData["Error"] = "You have already submitted this assignment.";
                return RedirectToAction(nameof(StudentDetails), new { id });
            }

            return View(assignment);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int id,
            string SubmissionText)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(StudentController.ProfileSetupRequired), "Student");

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            var existingSubmission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == id &&
                    s.StudentId == student.StudentId);

            if (existingSubmission != null)
            {
                TempData["Error"] = "You have already submitted this assignment.";
                return RedirectToAction(nameof(StudentDetails), new { id });
            }

            if (string.IsNullOrWhiteSpace(SubmissionText))
            {
                ModelState.AddModelError(
                    "SubmissionText",
                    "Please enter your submission.");

                return View(assignment);
            }

            var submission = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = student.StudentId,
                SubmissionText = SubmissionText,
                SubmittedDate = DateTime.Now,
                Status = "Submitted"
            };

            _context.AssignmentSubmissions.Add(submission);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment submitted successfully.";

            return RedirectToAction(nameof(StudentDetails), new { id });
        }

        // =========================================================
        // STUDENT - VIEW OWN SUBMISSION
        // =========================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MySubmission(int id)
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return RedirectToAction(nameof(StudentController.ProfileSetupRequired), "Student");

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == id &&
                    s.StudentId == student.StudentId);

            if (submission == null)
                return NotFound();

            return View(submission);
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
                .FirstOrDefaultAsync(t => t.TeacherId == applicationUser.TeacherId.Value);
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var applicationUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (applicationUser == null || !applicationUser.StudentId.HasValue)
                return null;

            return await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == applicationUser.StudentId.Value);
        }

        private async Task<string?> GetStudentUserIdAsync(int studentId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StudentId == studentId);

            return user?.Id;
        }

        private async Task NotifyStudentsOfNewAssignmentAsync(Assignment assignment)
        {
            var studentUserIds = await (
                from e in _context.Enrollments
                join u in _userManager.Users on e.StudentId equals u.StudentId
                where e.CourseId == assignment.CourseId
                select u.Id
            ).Distinct().ToListAsync();

            foreach (var userId in studentUserIds)
            {
                await AddNotificationAsync(
                    userId,
                    "New Assignment",
                    $"A new assignment \"{assignment.Title}\" has been posted.",
                    "Assignment",
                    Url.Action(nameof(StudentDetails), "Assignment", new { id = assignment.AssignmentId }));
            }
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
    }
}
