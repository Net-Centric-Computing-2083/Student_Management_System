using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(
            string? searchString,
            string? grade,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            var query = _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(r =>
                    (r.Student!.Name ?? "").Contains(searchString) ||
                    (r.Student.RegistrationNumber ?? "").Contains(searchString) ||
                    (r.Course!.CourseName ?? "").Contains(searchString) ||
                    (r.Course.CourseCode ?? "").Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(grade))
                query = query.Where(r => r.Grade == grade);

            query = sortOrder switch
            {
                "marks_asc" => query.OrderBy(r => r.TotalMarks),
                "marks_desc" => query.OrderByDescending(r => r.TotalMarks),
                "student_desc" => query.OrderByDescending(r => r.Student!.Name),
                _ => query.OrderBy(r => r.Student!.Name).ThenBy(r => r.Course!.CourseName)
            };

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentGrade"] = grade;
            ViewData["CurrentSort"] = sortOrder;
            ViewBag.Grades = await _context.Results
                .Select(r => r.Grade)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return View(await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var result = await _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            return result == null ? NotFound() : View(result);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Result { ResultDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Result result)
        {
            await ValidateResultAsync(result);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(result.StudentId, result.CourseId);
                return View(result);
            }

            CalculateResult(result);
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Result saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var result = await _context.Results.FindAsync(id);
            if (result == null) return NotFound();

            await LoadDropdownsAsync(result.StudentId, result.CourseId);
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Result result)
        {
            if (id != result.ResultId) return NotFound();

            await ValidateResultAsync(result, result.ResultId);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(result.StudentId, result.CourseId);
                return View(result);
            }

            CalculateResult(result);

            try
            {
                _context.Update(result);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Results.AnyAsync(r => r.ResultId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Result updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var result = await _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            return result == null ? NotFound() : View(result);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null) return NotFound();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Result deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateResultAsync(Result result, int? excludeId = null)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.StudentId == result.StudentId);
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == result.CourseId);

            if (!studentExists) ModelState.AddModelError(nameof(Result.StudentId), "Select a valid student.");
            if (!courseExists) ModelState.AddModelError(nameof(Result.CourseId), "Select a valid course.");

            if (studentExists && courseExists)
            {
                var enrolled = await _context.Enrollments.AnyAsync(e =>
                    e.StudentId == result.StudentId && e.CourseId == result.CourseId);

                if (!enrolled)
                    ModelState.AddModelError("", "The selected student is not enrolled in the selected course.");
            }

            if (await _context.Results.AnyAsync(r =>
                r.ResultId != excludeId &&
                r.StudentId == result.StudentId &&
                r.CourseId == result.CourseId))
            {
                ModelState.AddModelError("", "A result already exists for this student and course.");
            }
        }

        private static void CalculateResult(Result result)
        {
            result.TotalMarks = result.InternalMarks + result.PracticalMarks + result.FinalMarks;
            result.Grade = result.TotalMarks switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B+",
                >= 60 => "B",
                >= 50 => "C+",
                >= 40 => "C",
                >= 30 => "D",
                _ => "F"
            };
        }

        private async Task LoadDropdownsAsync(int? studentId = null, int? courseId = null)
        {
            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);

            ViewData["CourseId"] = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);
        }
    }
}
