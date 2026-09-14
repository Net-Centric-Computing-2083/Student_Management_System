using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    public class ResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Results
        public async Task<IActionResult> Index()
        {
            var results = await _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .OrderBy(r => r.Student!.Name)
                .ThenBy(r => r.Course!.CourseName)
                .ToListAsync();

            return View(results);
        }

        // GET: Results/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }

        // GET: Results/Create
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        // POST: Results/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Result result)
        {
            // Check whether the selected student and course are enrolled together
            bool isEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == result.StudentId &&
                    e.CourseId == result.CourseId);

            if (!isEnrolled)
            {
                ModelState.AddModelError(
                    "",
                    "The selected student is not enrolled in the selected course.");
            }

            // Prevent duplicate result for the same student and course
            bool duplicateResult = await _context.Results
                .AnyAsync(r =>
                    r.StudentId == result.StudentId &&
                    r.CourseId == result.CourseId);

            if (duplicateResult)
            {
                ModelState.AddModelError(
                    "",
                    "A result already exists for this student and course.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(result);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(result.StudentId, result.CourseId);

            return View(result);
        }

        // GET: Results/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.Results.FindAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            LoadDropdowns(result.StudentId, result.CourseId);

            return View(result);
        }

        // POST: Results/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Result result)
        {
            if (id != result.ResultId)
            {
                return NotFound();
            }

            bool duplicateResult = await _context.Results
                .AnyAsync(r =>
                    r.ResultId != result.ResultId &&
                    r.StudentId == result.StudentId &&
                    r.CourseId == result.CourseId);

            if (duplicateResult)
            {
                ModelState.AddModelError(
                    "",
                    "A result already exists for this student and course.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(result);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResultExists(result.ResultId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(result.StudentId, result.CourseId);

            return View(result);
        }

        // GET: Results/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }

        // POST: Results/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _context.Results.FindAsync(id);

            if (result != null)
            {
                _context.Results.Remove(result);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ResultExists(int id)
        {
            return _context.Results.Any(e => e.ResultId == id);
        }

        private void LoadDropdowns(
            int? selectedStudentId = null,
            int? selectedCourseId = null)
        {
            ViewData["StudentId"] = new SelectList(
                _context.Students.OrderBy(s => s.Name),
                "StudentId",
                "Name",
                selectedStudentId);

            ViewData["CourseId"] = new SelectList(
                _context.Courses.OrderBy(c => c.CourseName),
                "CourseId",
                "CourseName",
                selectedCourseId);
        }
    }
}