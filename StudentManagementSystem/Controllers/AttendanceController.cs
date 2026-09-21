using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AttendanceController : Controller
    {
        private static readonly string[] AllowedStatuses = { "Present", "Absent", "Late" };
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(
            int? courseId,
            int? semesterId,
            DateTime? attendanceDate)
        {
            var date = attendanceDate?.Date ?? DateTime.Today;
            var model = new AttendanceMarkingViewModel
            {
                CourseId = courseId ?? 0,
                SemesterId = semesterId ?? 0,
                AttendanceDate = date
            };

            await LoadBatchDropdownsAsync(model.CourseId, model.SemesterId);

            var students = await _context.Students
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            var existing = model.CourseId > 0 && model.SemesterId > 0
                ? await _context.Attendances
                    .Where(a => a.CourseId == model.CourseId &&
                                a.SemesterId == model.SemesterId &&
                                a.AttendanceDate == date)
                    .ToListAsync()
                : new List<Attendance>();

            model.Students = students.Select(s =>
            {
                var record = existing.FirstOrDefault(a => a.StudentId == s.StudentId);
                return new StudentAttendanceViewModel
                {
                    StudentId = s.StudentId,
                    StudentName = s.Name,
                    IsPresent = record?.Status == "Present"
                };
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AttendanceMarkingViewModel model)
        {
            if (!await _context.Courses.AnyAsync(c => c.CourseId == model.CourseId))
                ModelState.AddModelError(nameof(model.CourseId), "Select a valid course.");

            if (!await _context.Semesters.AnyAsync(s => s.SemesterId == model.SemesterId))
                ModelState.AddModelError(nameof(model.SemesterId), "Select a valid semester.");

            if (model.AttendanceDate.Date > DateTime.Today)
                ModelState.AddModelError(nameof(model.AttendanceDate), "Attendance date cannot be in the future.");

            if (!ModelState.IsValid)
            {
                await LoadBatchDropdownsAsync(model.CourseId, model.SemesterId);
                return View(model);
            }

            var validStudentIds = await _context.Students
                .Select(s => s.StudentId)
                .ToHashSetAsync();

            model.Students = model.Students
                .Where(s => validStudentIds.Contains(s.StudentId))
                .ToList();

            var date = model.AttendanceDate.Date;
            var existing = await _context.Attendances
                .Where(a => a.CourseId == model.CourseId &&
                            a.SemesterId == model.SemesterId &&
                            a.AttendanceDate == date)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                var record = existing.FirstOrDefault(a => a.StudentId == student.StudentId);
                if (record == null)
                {
                    _context.Attendances.Add(new Attendance
                    {
                        StudentId = student.StudentId,
                        CourseId = model.CourseId,
                        SemesterId = model.SemesterId,
                        AttendanceDate = date,
                        Status = student.IsPresent ? "Present" : "Absent"
                    });
                }
                else
                {
                    record.Status = student.IsPresent ? "Present" : "Absent";
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Attendance saved successfully.";

            return RedirectToAction(nameof(Index), new
            {
                courseId = model.CourseId,
                semesterId = model.SemesterId,
                attendanceDate = date.ToString("yyyy-MM-dd")
            });
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Attendance { AttendanceDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Attendance attendance)
        {
            if (!AllowedStatuses.Contains(attendance.Status, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(attendance.Status), "Select a valid attendance status.");

            if (attendance.AttendanceDate.Date > DateTime.Today)
                ModelState.AddModelError(nameof(attendance.AttendanceDate), "Attendance date cannot be in the future.");

            var validStudent = await _context.Students.AnyAsync(s => s.StudentId == attendance.StudentId);
            var validCourse = await _context.Courses.AnyAsync(c => c.CourseId == attendance.CourseId);
            var validSemester = await _context.Semesters.AnyAsync(s => s.SemesterId == attendance.SemesterId);

            if (!validStudent) ModelState.AddModelError(nameof(attendance.StudentId), "Select a valid student.");
            if (!validCourse) ModelState.AddModelError(nameof(attendance.CourseId), "Select a valid course.");
            if (!validSemester) ModelState.AddModelError(nameof(attendance.SemesterId), "Select a valid semester.");

            if (await _context.Attendances.AnyAsync(a =>
                a.StudentId == attendance.StudentId &&
                a.CourseId == attendance.CourseId &&
                a.AttendanceDate == attendance.AttendanceDate.Date))
            {
                ModelState.AddModelError("", "Attendance for this student, course, and date already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(attendance.StudentId, attendance.CourseId, attendance.SemesterId);
                return View(attendance);
            }

            attendance.AttendanceDate = attendance.AttendanceDate.Date;
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attendance record created successfully.";
            return RedirectToAction(nameof(History));
        }

        public async Task<IActionResult> History(
            int? courseId,
            int? semesterId,
            DateTime? date)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Include(a => a.Semester)
                .AsNoTracking()
                .AsQueryable();

            if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId.Value);
            if (semesterId.HasValue) query = query.Where(a => a.SemesterId == semesterId.Value);
            if (date.HasValue) query = query.Where(a => a.AttendanceDate == date.Value.Date);

            ViewBag.Courses = new SelectList(await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(), "CourseId", "CourseName", courseId);
            ViewBag.Semesters = new SelectList(
                await _context.Semesters.OrderByDescending(s => s.SemesterId)
                    .Select(s => new { s.SemesterId, Display = s.AcademicYear + " - " + s.SemesterName })
                    .ToListAsync(), "SemesterId", "Display", semesterId);
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");

            return View(await query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Course!.CourseName)
                .ThenBy(a => a.Student!.Name)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);
            return attendance == null ? NotFound() : View(attendance);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return NotFound();

            await LoadDropdownsAsync(attendance.StudentId, attendance.CourseId, attendance.SemesterId);
            return View(attendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Attendance attendance)
        {
            if (id != attendance.AttendanceId) return NotFound();

            if (!AllowedStatuses.Contains(attendance.Status, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(attendance.Status), "Select a valid attendance status.");

            if (!await _context.Students.AnyAsync(s => s.StudentId == attendance.StudentId))
                ModelState.AddModelError(nameof(attendance.StudentId), "Select a valid student.");
            if (!await _context.Courses.AnyAsync(c => c.CourseId == attendance.CourseId))
                ModelState.AddModelError(nameof(attendance.CourseId), "Select a valid course.");
            if (!await _context.Semesters.AnyAsync(s => s.SemesterId == attendance.SemesterId))
                ModelState.AddModelError(nameof(attendance.SemesterId), "Select a valid semester.");

            if (await _context.Attendances.AnyAsync(a =>
                a.AttendanceId != id &&
                a.StudentId == attendance.StudentId &&
                a.CourseId == attendance.CourseId &&
                a.AttendanceDate == attendance.AttendanceDate.Date))
            {
                ModelState.AddModelError("", "Attendance for this student, course, and date already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(attendance.StudentId, attendance.CourseId, attendance.SemesterId);
                return View(attendance);
            }

            attendance.AttendanceDate = attendance.AttendanceDate.Date;

            try
            {
                _context.Update(attendance);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Attendances.AnyAsync(a => a.AttendanceId == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Attendance updated successfully.";
            return RedirectToAction(nameof(History));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);
            return attendance == null ? NotFound() : View(attendance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return NotFound();

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attendance record deleted successfully.";
            return RedirectToAction(nameof(History));
        }

        private async Task LoadBatchDropdownsAsync(int courseId, int semesterId)
        {
            ViewBag.Courses = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);

            ViewBag.Semesters = new SelectList(
                await _context.Semesters.OrderByDescending(s => s.SemesterId)
                    .Select(s => new { s.SemesterId, Display = s.AcademicYear + " - " + s.SemesterName })
                    .ToListAsync(),
                "SemesterId", "Display", semesterId);
        }

        private async Task LoadDropdownsAsync(int? studentId = null, int? courseId = null, int? semesterId = null)
        {
            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.Name).ToListAsync(),
                "StudentId", "Name", studentId);
            ViewData["CourseId"] = new SelectList(
                await _context.Courses.OrderBy(c => c.CourseName).ToListAsync(),
                "CourseId", "CourseName", courseId);
            ViewData["SemesterId"] = new SelectList(
                await _context.Semesters.OrderByDescending(s => s.SemesterId)
                    .Select(s => new { s.SemesterId, Display = s.AcademicYear + " - " + s.SemesterName })
                    .ToListAsync(),
                "SemesterId", "Display", semesterId);
        }
    }
}
