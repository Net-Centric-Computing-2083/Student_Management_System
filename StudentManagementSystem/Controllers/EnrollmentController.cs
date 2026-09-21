using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(
            string? searchString,
            int? semesterId,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            var enrollments = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Include(e => e.Semester)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                enrollments = enrollments.Where(e =>
                    (e.Student!.Name ?? "").Contains(searchString) ||
                    (e.Student.RegistrationNumber ?? "").Contains(searchString) ||
                    (e.Course!.CourseName ?? "").Contains(searchString) ||
                    (e.Course.CourseCode ?? "").Contains(searchString));
            }

            if (semesterId.HasValue)
                enrollments = enrollments.Where(e => e.SemesterId == semesterId.Value);

            enrollments = sortOrder switch
            {
                "date_desc" => enrollments.OrderByDescending(e => e.EnrollmentDate),
                "student" => enrollments.OrderBy(e => e.Student!.Name),
                "course" => enrollments.OrderBy(e => e.Course!.CourseName),
                _ => enrollments.OrderByDescending(e => e.EnrollmentDate)
            };

            var total = await enrollments.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var list = await enrollments.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSemester"] = semesterId;
            ViewData["CurrentSort"] = sortOrder;
            ViewBag.Semesters = new SelectList(
                await _context.Semesters.OrderByDescending(s => s.SemesterId).ToListAsync(),
                "SemesterId", "SemesterName", semesterId);

            return View(list);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Include(e => e.Semester)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            return enrollment == null ? NotFound() : View(enrollment);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Enrollment enrollment)
        {
            await ValidateEnrollmentAsync(enrollment);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(enrollment.StudentId, enrollment.CourseId, enrollment.SemesterId);
                return View(enrollment);
            }

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Enrollment created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null) return NotFound();

            await LoadDropdownsAsync(enrollment.StudentId, enrollment.CourseId, enrollment.SemesterId);
            return View(enrollment);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId) return NotFound();

            await ValidateEnrollmentAsync(enrollment, enrollment.EnrollmentId);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(enrollment.StudentId, enrollment.CourseId, enrollment.SemesterId);
                return View(enrollment);
            }

            try
            {
                _context.Update(enrollment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Enrollments.AnyAsync(e => e.EnrollmentId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Enrollment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Include(e => e.Semester)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            return enrollment == null ? NotFound() : View(enrollment);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null) return NotFound();

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Enrollment deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateEnrollmentAsync(Enrollment enrollment, int? excludeId = null)
        {
            if (!await _context.Students.AnyAsync(s => s.StudentId == enrollment.StudentId))
                ModelState.AddModelError(nameof(Enrollment.StudentId), "Select a valid student.");

            if (!await _context.Courses.AnyAsync(c => c.CourseId == enrollment.CourseId))
                ModelState.AddModelError(nameof(Enrollment.CourseId), "Select a valid course.");

            if (!await _context.Semesters.AnyAsync(s => s.SemesterId == enrollment.SemesterId))
                ModelState.AddModelError(nameof(Enrollment.SemesterId), "Select a valid semester.");

            if (await _context.Enrollments.AnyAsync(e =>
                e.StudentId == enrollment.StudentId &&
                e.CourseId == enrollment.CourseId &&
                e.SemesterId == enrollment.SemesterId &&
                e.EnrollmentId != excludeId))
            {
                ModelState.AddModelError("", "This student is already enrolled in this course for the selected semester.");
            }
        }

        private async Task LoadDropdownsAsync(
            int? studentId = null,
            int? courseId = null,
            int? semesterId = null)
        {
            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);

            ViewData["CourseId"] = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);

            ViewData["SemesterId"] = new SelectList(
                await _context.Semesters.OrderByDescending(s => s.SemesterId)
                    .Select(s => new
                    {
                        s.SemesterId,
                        Display = s.AcademicYear + " - " + s.SemesterName
                    }).ToListAsync(),
                "SemesterId", "Display", semesterId);
        }
    }
}
