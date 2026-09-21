using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(
            string? searchString,
            int? departmentId,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 8;
            page = Math.Max(page, 1);

            var courses = _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                courses = courses.Where(c =>
                    (c.CourseName ?? "").Contains(searchString) ||
                    (c.CourseCode ?? "").Contains(searchString));
            }

            if (departmentId.HasValue)
                courses = courses.Where(c => c.DepartmentId == departmentId.Value);

            courses = sortOrder switch
            {
                "name_desc" => courses.OrderByDescending(c => c.CourseName),
                "code" => courses.OrderBy(c => c.CourseCode),
                "code_desc" => courses.OrderByDescending(c => c.CourseCode),
                "credits" => courses.OrderBy(c => c.CreditHours),
                "credits_desc" => courses.OrderByDescending(c => c.CreditHours),
                _ => courses.OrderBy(c => c.CourseName)
            };

            var total = await courses.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var list = await courses.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentDepartment"] = departmentId;
            ViewData["CurrentSort"] = sortOrder;
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync(),
                "DepartmentId", "DepartmentName", departmentId);

            return View(list);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var course = await _context.Courses.Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.CourseId == id);
            return course == null ? NotFound() : View(course);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadDepartmentsAsync();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CourseId,CourseName,CourseCode,CreditHours,DepartmentId")] Course course)
        {
            await ValidateCourseAsync(course);

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(course.DepartmentId);
                return View(course);
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            await LoadDepartmentsAsync(course.DepartmentId);
            return View(course);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CourseId,CourseName,CourseCode,CreditHours,DepartmentId")] Course course)
        {
            if (id != course.CourseId) return NotFound();

            await ValidateCourseAsync(course, course.CourseId);

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(course.DepartmentId);
                return View(course);
            }

            try
            {
                _context.Update(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Courses.AnyAsync(c => c.CourseId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var course = await _context.Courses.Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.CourseId == id);
            return course == null ? NotFound() : View(course);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            var hasAttendance = await _context.Attendances.AnyAsync(a => a.CourseId == id);
            var hasResults = await _context.Results.AnyAsync(r => r.CourseId == id);

            if (course.Enrollments.Count > 0 || hasAttendance || hasResults)
            {
                TempData["DeleteError"] =
                    "This course cannot be deleted because dependent enrollment, attendance, or result records exist.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateCourseAsync(Course course, int? excludeId = null)
        {
            if (!string.IsNullOrWhiteSpace(course.CourseCode) &&
                await _context.Courses.AnyAsync(c =>
                    c.CourseCode == course.CourseCode && c.CourseId != excludeId))
                ModelState.AddModelError(nameof(Course.CourseCode), "A course with this code already exists.");

            if (!string.IsNullOrWhiteSpace(course.CourseName) &&
                await _context.Courses.AnyAsync(c =>
                    c.CourseName == course.CourseName && c.CourseId != excludeId))
                ModelState.AddModelError(nameof(Course.CourseName), "A course with this name already exists.");

            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == course.DepartmentId))
                ModelState.AddModelError(nameof(Course.DepartmentId), "Select a valid department.");
        }

        private async Task LoadDepartmentsAsync(int? selected = null)
        {
            ViewData["DepartmentId"] = new SelectList(
                await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync(),
                "DepartmentId", "DepartmentName", selected);
        }
    }
}
