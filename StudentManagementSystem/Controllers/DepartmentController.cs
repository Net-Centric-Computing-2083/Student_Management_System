using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: Department
        // Search + Filter + Sort + Pagination
        // =========================================================
        public async Task<IActionResult> Index(
            string searchString,
            string departmentCode,
            string studentStatus,
            string sortOrder,
            int? page)
        {
            int pageSize = 5;
            int pageNumber = page ?? 1;

            var departments = _context.Departments
                .Include(d => d.Students)
                .Include(d => d.Courses)
                .AsQueryable();

            // -----------------------------------------------------
            // Search
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                departments = departments.Where(d =>
                    d.DepartmentName!.Contains(searchString) ||
                    d.DepartmentCode!.Contains(searchString) ||
                    d.HodName!.Contains(searchString));
            }

            // -----------------------------------------------------
            // Filter by Department Code
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                departments = departments.Where(d =>
                    d.DepartmentCode == departmentCode);
            }

            // -----------------------------------------------------
            // Filter by Student Status
            // -----------------------------------------------------
            if (studentStatus == "HasStudents")
            {
                departments = departments.Where(d =>
                    d.Students.Any());
            }
            else if (studentStatus == "NoStudents")
            {
                departments = departments.Where(d =>
                    !d.Students.Any());
            }

            // -----------------------------------------------------
            // Sorting
            // -----------------------------------------------------
            switch (sortOrder)
            {
                case "name":
                    departments = departments.OrderBy(
                        d => d.DepartmentName);
                    break;

                case "name_desc":
                    departments = departments.OrderByDescending(
                        d => d.DepartmentName);
                    break;

                case "code_asc":
                    departments = departments.OrderBy(
                        d => d.DepartmentCode);
                    break;

                case "code_desc":
                    departments = departments.OrderByDescending(
                        d => d.DepartmentCode);
                    break;

                case "hod_asc":
                    departments = departments.OrderBy(
                        d => d.HodName);
                    break;

                case "hod_desc":
                    departments = departments.OrderByDescending(
                        d => d.HodName);
                    break;

                case "students_asc":
                    departments = departments.OrderBy(
                        d => d.Students.Count);
                    break;

                case "students_desc":
                    departments = departments.OrderByDescending(
                        d => d.Students.Count);
                    break;

                case "courses_asc":
                    departments = departments.OrderBy(
                        d => d.Courses.Count);
                    break;

                case "courses_desc":
                    departments = departments.OrderByDescending(
                        d => d.Courses.Count);
                    break;

                default:
                    departments = departments.OrderBy(
                        d => d.DepartmentName);
                    break;
            }

            // -----------------------------------------------------
            // Total Departments after Search and Filters
            // -----------------------------------------------------
            int totalDepartments = await departments.CountAsync();

            // -----------------------------------------------------
            // Pagination
            // -----------------------------------------------------
            var departmentList = await departments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalPages = (int)Math.Ceiling(
                totalDepartments / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
                departmentList = await departments
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            // -----------------------------------------------------
            // Department Codes for Filter Dropdown
            // -----------------------------------------------------
            ViewBag.DepartmentCodes = await _context.Departments
                .Select(d => d.DepartmentCode)
                .Where(code => !string.IsNullOrEmpty(code))
                .Distinct()
                .OrderBy(code => code)
                .ToListAsync();

            // -----------------------------------------------------
            // Preserve Search / Filter / Sort Values
            // -----------------------------------------------------
            ViewData["SearchString"] = searchString;
            ViewData["DepartmentCode"] = departmentCode;
            ViewData["StudentStatus"] = studentStatus;
            ViewData["SortOrder"] = sortOrder;

            // -----------------------------------------------------
            // Pagination Information
            // -----------------------------------------------------
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalDepartments"] = totalDepartments;

            return View(departmentList);
        }


        // =========================================================
        // GET: Department/Details/5
        // =========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Students)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }


        // =========================================================
        // GET: Department/Students/5
        // View Students by Department
        // =========================================================
        public async Task<IActionResult> Students(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }


        // =========================================================
        // GET: Department/Create
        // =========================================================
        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // POST: Department/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DepartmentId,DepartmentName,DepartmentCode,HodName,Description")]
            Department department)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Departments.AnyAsync(d =>
                    d.DepartmentName == department.DepartmentName))
                    ModelState.AddModelError("DepartmentName", "A department with this name already exists.");

                if (await _context.Departments.AnyAsync(d =>
                    d.DepartmentCode == department.DepartmentCode))
                    ModelState.AddModelError("DepartmentCode", "A department with this code already exists.");
            }

            if (!ModelState.IsValid)
                return View(department);

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET: Department/Edit/5
        // =========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FindAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }


        // =========================================================
        // POST: Department/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("DepartmentId,DepartmentName,DepartmentCode,HodName,Description")]
            Department department)
        {
            if (id != department.DepartmentId)
                return NotFound();

            if (ModelState.IsValid)
            {
                if (await _context.Departments.AnyAsync(d =>
                    d.DepartmentName == department.DepartmentName &&
                    d.DepartmentId != id))
                    ModelState.AddModelError("DepartmentName", "A department with this name already exists.");

                if (await _context.Departments.AnyAsync(d =>
                    d.DepartmentCode == department.DepartmentCode &&
                    d.DepartmentId != id))
                    ModelState.AddModelError("DepartmentCode", "A department with this code already exists.");
            }

            if (!ModelState.IsValid)
                return View(department);

            try
            {
                _context.Update(department);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(department.DepartmentId))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET: Department/Delete/5
        // =========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }


        // =========================================================
        // POST: Department/Delete/5
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Students)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            // Prevent deletion if students or courses exist
            if (department.Students.Any() ||
                department.Courses.Any())
            {
                TempData["DeleteError"] =
                    "This department cannot be deleted because it has students or courses assigned to it.";

                return RedirectToAction(
                    nameof(Delete),
                    new { id });
            }

            _context.Departments.Remove(department);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // Check whether a department exists
        // =========================================================
        private bool DepartmentExists(int id)
        {
            return _context.Departments
                .Any(e => e.DepartmentId == id);
        }
    }
}