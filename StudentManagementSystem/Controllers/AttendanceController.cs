using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendance
        public async Task<IActionResult> Index(
            int? courseId,
            DateTime? attendanceDate)
        {
            var model = new AttendanceMarkingViewModel
            {
                CourseId = courseId ?? 0,
                AttendanceDate = attendanceDate?.Date ?? DateTime.Today
            };

            ViewBag.Courses = new SelectList(
                _context.Courses,
                "CourseId",
                "CourseName",
                model.CourseId
            );

            // Load all students
            var students = await _context.Students
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Load existing attendance for selected course and date
            var existingAttendance = new List<Attendance>();

            if (model.CourseId != 0)
            {
                existingAttendance = await _context.Attendances
                    .Where(a =>
                        a.CourseId == model.CourseId &&
                        a.AttendanceDate.Date == model.AttendanceDate.Date)
                    .ToListAsync();
            }

            // Create student attendance list
            model.Students = students.Select(student =>
            {
                var existing = existingAttendance
                    .FirstOrDefault(a =>
                        a.StudentId == student.StudentId);

                return new StudentAttendanceViewModel
                {
                    StudentId = student.StudentId,
                    StudentName = student.Name,
                    IsPresent = existing?.Status == "Present"
                };
            }).ToList();

            return View(model);
        }


        // POST: Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            AttendanceMarkingViewModel model)
        {
            if (model.CourseId == 0)
            {
                ModelState.AddModelError(
                    "CourseId",
                    "Please select a course."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(
                    _context.Courses,
                    "CourseId",
                    "CourseName",
                    model.CourseId
                );

                return View(model);
            }

            // Get existing attendance records
            var existingAttendance = await _context.Attendances
                .Where(a =>
                    a.CourseId == model.CourseId &&
                    a.AttendanceDate.Date == model.AttendanceDate.Date)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                // Check whether attendance already exists
                var existingRecord = existingAttendance
                    .FirstOrDefault(a =>
                        a.StudentId == student.StudentId);

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.Status =
                        student.IsPresent
                            ? "Present"
                            : "Absent";
                }
                else
                {
                    // Create new record only if it does not exist
                    var attendance = new Attendance
                    {
                        StudentId = student.StudentId,
                        CourseId = model.CourseId,
                        AttendanceDate =
                            model.AttendanceDate.Date,
                        Status =
                            student.IsPresent
                                ? "Present"
                                : "Absent"
                    };

                    _context.Attendances.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Attendance saved successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    courseId = model.CourseId,
                    attendanceDate =
                        model.AttendanceDate.ToString("yyyy-MM-dd")
                }
            );
        }


        // GET: Attendance/History
        public async Task<IActionResult> History()
        {
            var attendanceHistory =
                await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Course)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenBy(a => a.Course!.CourseName)
                    .ThenBy(a => a.Student!.Name)
                    .ToListAsync();

            return View(attendanceHistory);
        }


        // GET: Attendance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(
                    a => a.AttendanceId == id
                );

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }


        // GET: Attendance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance =
                await _context.Attendances.FindAsync(id);

            if (attendance == null)
            {
                return NotFound();
            }

            ViewData["StudentId"] = new SelectList(
                _context.Students,
                "StudentId",
                "Name",
                attendance.StudentId
            );

            ViewData["CourseId"] = new SelectList(
                _context.Courses,
                "CourseId",
                "CourseName",
                attendance.CourseId
            );

            return View(attendance);
        }


        // POST: Attendance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Attendance attendance)
        {
            if (id != attendance.AttendanceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(attendance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceExists(
                        attendance.AttendanceId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(
                    nameof(History)
                );
            }

            ViewData["StudentId"] = new SelectList(
                _context.Students,
                "StudentId",
                "Name",
                attendance.StudentId
            );

            ViewData["CourseId"] = new SelectList(
                _context.Courses,
                "CourseId",
                "CourseName",
                attendance.CourseId
            );

            return View(attendance);
        }


        // GET: Attendance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(
                    a => a.AttendanceId == id
                );

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }


        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var attendance =
                await _context.Attendances.FindAsync(id);

            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(History)
            );
        }


        private bool AttendanceExists(int id)
        {
            return _context.Attendances
                .Any(a => a.AttendanceId == id);
        }
    }
}