using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Student
    public async Task<IActionResult> Index(string searchString)
    {
        var students = from s in _context.Students
                       select s;

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            students = students.Where(s =>
                s.Name.Contains(searchString) ||
                s.Email.Contains(searchString) ||
                s.Phone.Contains(searchString) ||
                s.StudentId.ToString().Contains(searchString));
        }

        return View(await students.ToListAsync());
    }

    // GET: Student/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // GET: Student/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Student/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("StudentId,Name,Email,Phone,Address,Gender,DateOfBirth,DepartmentId")]
        Student student)
    {
        if (ModelState.IsValid)
        {
            // Check whether email already exists
            bool emailExists = await _context.Students
                .AnyAsync(s => s.Email.ToLower() == student.Email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "A student with this email already exists.");

                return View(student);
            }

            _context.Add(student);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(student);
    }

    // GET: Student/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Student/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("StudentId,Name,Email,Phone,Address,Gender,DateOfBirth,DepartmentId")]
        Student student)
    {
        // Make sure the route ID matches the student ID
        if (id == null || id != student.StudentId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check whether another student already uses this email
            bool emailExists = await _context.Students
                .AnyAsync(s =>
                    s.Email.ToLower() == student.Email.ToLower() &&
                    s.StudentId != student.StudentId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "A student with this email already exists.");

                return View(student);
            }

            try
            {
                _context.Update(student);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(student.StudentId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(student);
    }

    // GET: Student/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Student/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Check whether a student exists
    private bool StudentExists(int id)
    {
        return _context.Students.Any(e => e.StudentId == id);
    }
}