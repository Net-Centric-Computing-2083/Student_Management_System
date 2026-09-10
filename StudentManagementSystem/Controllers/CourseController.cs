using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Course
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Department)
                .ToListAsync();

            return View(courses);
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Course/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(
                _context.Departments,
                "DepartmentId",
                "DepartmentName");

            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CourseId,CourseName,CourseCode,CreditHours,DepartmentId")]
            Course course)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate Course Code
                bool courseCodeExists = await _context.Courses
                    .AnyAsync(c =>
                        c.CourseCode.ToLower() ==
                        course.CourseCode.ToLower());

                if (courseCodeExists)
                {
                    ModelState.AddModelError(
                        "CourseCode",
                        "A course with this course code already exists.");
                }

                // Check for duplicate Course Name
                bool courseNameExists = await _context.Courses
                    .AnyAsync(c =>
                        c.CourseName.ToLower() ==
                        course.CourseName.ToLower());

                if (courseNameExists)
                {
                    ModelState.AddModelError(
                        "CourseName",
                        "A course with this course name already exists.");
                }

                // If duplicate validation failed
                if (!ModelState.IsValid)
                {
                    ViewData["DepartmentId"] = new SelectList(
                        _context.Departments,
                        "DepartmentId",
                        "DepartmentName",
                        course.DepartmentId);

                    return View(course);
                }

                _context.Add(course);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(
                _context.Departments,
                "DepartmentId",
                "DepartmentName",
                course.DepartmentId);

            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            ViewData["DepartmentId"] = new SelectList(
                _context.Departments,
                "DepartmentId",
                "DepartmentName",
                course.DepartmentId);

            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CourseId,CourseName,CourseCode,CreditHours,DepartmentId")]
            Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check for duplicate Course Code
                bool courseCodeExists = await _context.Courses
                    .AnyAsync(c =>
                        c.CourseCode.ToLower() ==
                        course.CourseCode.ToLower() &&
                        c.CourseId != course.CourseId);

                if (courseCodeExists)
                {
                    ModelState.AddModelError(
                        "CourseCode",
                        "A course with this course code already exists.");
                }

                // Check for duplicate Course Name
                bool courseNameExists = await _context.Courses
                    .AnyAsync(c =>
                        c.CourseName.ToLower() ==
                        course.CourseName.ToLower() &&
                        c.CourseId != course.CourseId);

                if (courseNameExists)
                {
                    ModelState.AddModelError(
                        "CourseName",
                        "A course with this course name already exists.");
                }

                // If duplicate validation failed
                if (!ModelState.IsValid)
                {
                    ViewData["DepartmentId"] = new SelectList(
                        _context.Departments,
                        "DepartmentId",
                        "DepartmentName",
                        course.DepartmentId);

                    return View(course);
                }

                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(
                _context.Departments,
                "DepartmentId",
                "DepartmentName",
                course.DepartmentId);

            return View(course);
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Check whether a course exists
        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}