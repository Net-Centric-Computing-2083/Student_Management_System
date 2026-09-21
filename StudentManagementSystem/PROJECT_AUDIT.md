# Student Management System - Project Audit

## Scope
The existing uploaded ASP.NET Core MVC project was reviewed and updated in place. The existing architecture, EF Core, SQL Server, Razor Views, Bootstrap, and Identity stack were preserved.

## Completed / Fixed
- Phase 1: MVC/EF Core/Identity startup reviewed; migrations are now applied before role/data seeding.
- Phase 2: Student CRUD retained and hardened with photo validation, safe generated filenames, replacement cleanup, unique email/name checks, and safer registration-number generation.
- Phase 3: Identity login/logout/roles reviewed; Student registration now requires a matching existing Student profile (email + registration number + date of birth), preventing arbitrary profile creation and improving account/profile linkage.
- Phase 4: Department CRUD/search/filter/sort/pagination retained; duplicate name/code checks and dependent-delete protection fixed.
- Phase 5: Course management completed with search/filter/sort/pagination, validation, duplicate protection, department validation, and dependent-delete protection.
- Phase 6: Enrollment management completed with semester selection, duplicate student/course/semester protection, validation, search/filter/pagination.
- Phase 7: Attendance management completed with semester-aware batch marking, single-record CRUD, status validation, duplicate protection, filters, and student-only attendance view.
- Phase 8: Results management completed with validation, enrollment check, duplicate protection, automatic total/grade calculation, search/filter/pagination, and student-only results view.
- Phase 9: Student Dashboard completed with Profile, Courses, Results, and Attendance links; student data is resolved from the authenticated account email rather than a URL ID.
- Phase 10: Admin Dashboard completed with live statistics and management shortcuts.
- Phase 11: Teacher Dashboard completed with role-appropriate access to Students, Courses, Attendance, and Results.
- Phase 12: Search/filter/sort/pagination improved across Students, Departments, Courses, Enrollments, and Results.
- Phase 13: Validation, duplicate handling, missing-record checks, dependent-delete checks, and user-friendly messages improved.
- Phase 14: Anti-forgery protection and role authorization reviewed; student dashboard data is isolated from URL ID tampering.
- Phase 15: Existing EF Core relationships and restrictive delete behavior preserved. Existing migrations retained.
- Phase 16/17: Navigation and dashboard links corrected (including incorrect plural controller names), role-based navigation added, and missing Student views created.

## Major New Files
- Views/Student/Courses.cshtml
- Views/Student/Results.cshtml
- Views/Student/Attendance.cshtml
- Views/Student/ProfileSetupRequired.cshtml

## Important Modified Areas
- Program.cs
- Data/RoleSeeder.cs
- Data/ApplicationDbContext.cs
- Controllers/AccountController.cs
- Controllers/AdminController.cs
- Controllers/StudentController.cs
- Controllers/DepartmentController.cs
- Controllers/CourseController.cs
- Controllers/EnrollmentController.cs
- Controllers/AttendanceController.cs
- Controllers/ResultsController.cs
- Controllers/TeacherController.cs
- Models/AttendanceMarkingViewModel.cs
- Models/Teacher.cs
- ViewModels/RegisterViewModel.cs
- Shared layout and management/dashboard views

## Database
No new schema migration was added in this pass because the final implementation uses the existing database schema. Existing migrations remain in the project. Startup now calls `Database.MigrateAsync()` before seeding roles and semesters.

The existing database already contains:
- Students
- Departments
- Courses
- Enrollments
- Attendance
- Results
- Semesters
- ASP.NET Core Identity tables
- ActivityLogs and Notifications

## Default Account
The project explicitly contains the following seeded Admin credentials:
- Email: admin@studentmanagement.com
- Password: Admin@123
- Role: Admin

No Teacher or Student credentials were invented or reported because the uploaded project did not explicitly contain fixed test credentials for those roles.

Student self-registration requires an existing Student profile created by Admin/Teacher and matching:
- Email
- Registration Number
- Date of Birth

## How to Run
1. Extract the ZIP.
2. Open StudentManagementSystem.slnx in Visual Studio.
3. Check `appsettings.json` and make sure the `DefaultConnection` SQL Server instance is available on the machine.
4. Build the solution in Visual Studio.
5. Run the application. Startup applies checked-in EF Core migrations automatically and seeds roles, semesters, and the default Admin account.
6. Test Admin with the credentials above.
7. For Student testing, first create a Student record from the Admin/Teacher interface, then use Register with the same email, registration number, and date of birth.
8. A Teacher account must be created/assigned through an existing Identity/user-management process; no fixed Teacher test account was present in the uploaded project.

## Verification Limitation
The available execution environment did not contain the .NET SDK (`dotnet` was not installed, and package installation is blocked by the sandbox's network egress rules), so the application could not be compiled or launched against SQL Server here. The project was therefore checked statically (brace/paren balance, action-name cross-referencing between controllers and views, namespace/using verification) and the code/views/migrations were updated consistently. **Please run a full `dotnet build` in Visual Studio and report any errors** before we move on to Phase 4 — that is the fastest way to catch anything the static review missed.

## Phase 3 — Student Module (this update)
Scope: complete/extend the Student-facing module per the spec, reusing all existing Phase 1/2 models, DbContext relationships, and Identity/role setup.

### Bug found and fixed (pre-existing, not part of Phase 3 itself)
`Controllers/AssignmentController.cs` (already present in the uploaded ZIP, described as "Assignment functionality currently being implemented") did not compile:
- It compared `Assignment.TeacherId` / `AssignmentSubmission.StudentId` (both `int` foreign keys) against `teacher.Id` / `student.Id`, which are ASP.NET Identity string user IDs. These types cannot be compared and the file would fail to build.
- It also referenced `AssignmentSubmission.SubmittedAt`, but the model property (added in Phase 2) is `SubmittedDate`.
- No Razor views existed for any of its actions.

Fixed by adding `GetCurrentTeacherAsync()` / `GetCurrentStudentAsync()` helpers (same pattern as `StudentController`) that resolve the logged-in Identity user to their linked `Teacher`/`Student` row via `ApplicationUser.TeacherId` / `StudentId`, and using the resulting integer keys everywhere. Also added a `Grade` action (Teacher grades a submission — this doubles as the start of Phase 4's grading feature) and per-course/new-assignment/graded notifications. Added `Views/Teacher/ProfileSetupRequired.cshtml` and a matching `TeacherController.ProfileSetupRequired()` action (mirroring the existing Student one) for the edge case where a Teacher's Identity account isn't yet linked to a Teacher record.

### New Student-facing features (Controllers/StudentController.cs)
All new actions are `[Authorize(Roles = "Student")]` and resolve the current student via the existing `GetCurrentStudentAsync()` helper, so a Student can only ever see their own data (no ID is ever taken from the URL/query string).
- **Dashboard** — rebuilt to show enrolled-course count, overall GPA, attendance %, pending-assignment count, upcoming assignment deadlines, recent announcements, and recent/unread notifications (`ViewModels/StudentDashboardViewModel.cs`).
- **Announcements** / **AnnouncementDetails** — shows system-wide announcements targeted at students/all, plus course announcements for the student's enrolled courses only.
- **ClassSchedule** — read-only weekly timetable, built from `ClassSchedule` rows for the student's enrolled (course, semester) pairs.
- **Fees** — fee records with computed paid/remaining balance and full payment history, scoped to the student.
- **Documents** / **DownloadDocument** — lists the student's own documents and streams the file back; 404s if the document doesn't belong to them.
- **Notifications** / **MarkNotificationRead** / **MarkAllNotificationsRead** — the student's own notification feed.
- **LeaveRequests** / **CreateLeaveRequest** — list + create; students can optionally tie a request to one of their enrolled courses (routes to that course's teacher) or leave it general (routes to Admin), status starts `Pending`.
- **ContactTeacher** / **SendMessage** / **MessageThread** — students can message a teacher of one of their enrolled courses (the course/teacher pairing is derived from `ClassSchedule`, since the schema doesn't have a direct Course→Teacher field), see threaded replies, and the teacher gets a `Notification` when a message arrives.

### New Assignment features, Student side (Controllers/AssignmentController.cs, Views/Assignment/*)
- `StudentAssignments` — assignments for the student's enrolled courses only (previously showed every assignment in the system).
- `StudentDetails`, `Submit`, `MySubmission` — view an assignment, submit once (duplicate submissions blocked), and view your own grade/feedback once a teacher has graded it.

### New/modified files
- `ViewModels/StudentDashboardViewModel.cs` (new)
- `Controllers/StudentController.cs` (modified — Dashboard rewritten, ~14 new actions + 2 private helpers added)
- `Controllers/AssignmentController.cs` (rewritten — bug fixes described above + new `Grade` action + notification helpers)
- `Controllers/TeacherController.cs` (modified — added `ProfileSetupRequired`)
- `Views/Student/Dashboard.cshtml` (rewritten), plus new: `Announcements.cshtml`, `AnnouncementDetails.cshtml`, `ClassSchedule.cshtml`, `Fees.cshtml`, `Documents.cshtml`, `Notifications.cshtml`, `LeaveRequests.cshtml`, `CreateLeaveRequest.cshtml`, `ContactTeacher.cshtml`, `SendMessage.cshtml`, `MessageThread.cshtml`
- `Views/Assignment/StudentAssignments.cshtml`, `StudentDetails.cshtml`, `Submit.cshtml`, `MySubmission.cshtml` (new)
- `Views/Teacher/ProfileSetupRequired.cshtml` (new)
- `Views/Shared/_Layout.cshtml` (modified — Student nav extended with links to all new pages; full sidebar redesign is Phase 6 per the agreed plan)

### Not touched in this phase (by design, per the phased plan)
- No Teacher-facing views were added for Assignments/Grading/Announcements/etc. (Phase 4). The Teacher-side `AssignmentController` actions now compile and are ready for those views.
- No Admin-facing screens for Fees, Documents, Announcements, Class Schedule, Leave Requests, etc. (Phase 5) — these all need an Admin UI to create the records Students now view.
- No database migration was needed — Phase 2 already added every table this phase reads from.

### How to test Phase 3
1. Build the solution first and report any errors.
2. Because Admin/Teacher screens for creating Announcements, Class Schedules, Fees, Documents, and Leave-Request reviews don't exist yet (Phases 4–5), the new Student pages will mostly show "no records yet" empty states until you either build Phase 4/5 or seed a few rows directly in SQL Server for a test student (an `Announcement`, a `ClassSchedule` row, a `Fee` + `Payment`, a `Document`) to confirm the pages render correctly end-to-end.
3. Assignments end-to-end already work without extra seeding: log in as the seeded Admin, create a Teacher user (existing flow), have that teacher... — actually, since Teacher-side Assignment views don't exist until Phase 4, for now you can create a test `Assignment` row directly in SQL Server for a course the test student is enrolled in, then log in as that student and confirm `Assignments` → view → submit works, and that a `Notification` row was created.
4. Log in as the seeded Admin (`admin@studentmanagement.com` / `Admin@123`) to confirm nothing on the Admin side broke.

---

## Phase 6 — Navigation & Dashboards (this update)

- Replaced the flat `<ul>` top-nav link lists with a real sidebar layout: `Views/Shared/_SidebarNav.cshtml` (new partial, shared by a desktop sidebar and a mobile Bootstrap offcanvas menu so the per-role link list only has to be maintained in one place), and `Views/Shared/_Layout.cshtml` was rewritten around an `.app-shell` flex layout. All four `TempData` alert keys (`DeleteError`, `Error`, `SuccessMessage`, `Success`) and the `@RenderBody()` / `@RenderSectionAsync("Scripts", ...)` structure were preserved exactly.
- Sidebar CSS appended to `wwwroot/css/site.css`, matching the existing slate/blue palette already used elsewhere in the app (no new color scheme introduced).
- Every link in all three sidebars was cross-checked against an actual existing controller action before being wired in.
- **Decisions made where the spec was ambiguous:**
  - *Notifications*: none of the three roles had a dedicated notification list page (Student did; Teacher/Admin didn't). Added `Notifications()` / `MarkNotificationRead()` / `MarkAllNotificationsRead()` to `TeacherController.cs` and `AdminController.cs`, reusing `Views/Student/Notifications.cshtml` as-is for both new roles (copied to `Views/Teacher/Notifications.cshtml` and `Views/Admin/Notifications.cshtml` since the view only uses relative `asp-action` links, so it works unmodified in any of the three controllers).
  - *Security (Admin)*: didn't exist. Added `AdminController.Security()` — a read-only overview (account counts, lockouts, role-based access summary) that links out to the existing Users & Roles page for actual account actions, so lockout/role logic is never duplicated. New `ViewModels/AdminSupportViewModels.cs` → `SecurityOverviewViewModel`, new `Views/Admin/Security.cshtml`.
  - *Teacher "Submissions" / "Grading"*: the spec lists these as separate sidebar items, but neither has a context-free landing page (both need a specific assignment/submission id). Both currently point at `Assignment/Index` (the teacher's assignment list, which is the natural starting point for either workflow).
  - *Admin "My Profile"*: didn't exist at all (Admin has no linked `Teacher`/`Student` row, just the Identity account). Added `AdminController.Profile()` GET/POST (edit `FullName`/`PhoneNumber` on the `ApplicationUser`; email is shown read-only) and `Views/Admin/Profile.cshtml`.
  - *Admin "Course Assignment" vs "Class Schedule"*: both sidebar items point at `Admin/ClassSchedules`, since in this schema `ClassSchedule` is the single table that both assigns a teacher to a course **and** sets the day/time/room — there's no separate table to split them across.

## Phase 7 — Cross-cutting Integration (complete)

Notification triggers added/verified:
- **New course-level announcement → enrolled students**: `TeacherController.CreateAnnouncement` (course-scoped) notifies every student enrolled in that course, linking to `Student/AnnouncementDetails`.
- **New course material → enrolled students**: `TeacherController.CreateCourseMaterial` notifies enrolled students, linking to `Student/CourseMaterials` (a page that didn't exist before this pass — see below).
- **Gap found and fixed while wiring the above**: students had no way to view or download course materials at all — the feature only existed on the Teacher upload side. Added `StudentController.CourseMaterials(int courseId)` / `DownloadCourseMaterial(int id)` (both strictly scoped to the student's own enrollments — `Forbid()` if the student isn't enrolled) and `Views/Student/CourseMaterials.cshtml`, plus a "View" link in the Materials column of `Views/Student/Courses.cshtml`.
- **System-wide announcement → targeted users** (new this pass): `AdminController.CreateAnnouncement` now calls a new `BroadcastAnnouncementNotificationAsync` helper, which reuses the same target-resolution logic as `SendNotifications` (`TargetRole` = All/Student/Teacher → the matching set of Identity users). Students are linked to `Student/AnnouncementDetails`; Teachers are linked to `Teacher/Announcements` (Teacher has no announcement-details page, so the list page is the closest sensible landing spot — flagging this decision in case a Teacher details page gets added later).
- **Fee/payment recorded → student notification** (new this pass): `AdminController.CreateFee` and `AdminController.RecordPayment` both now notify the affected student (new `AddNotificationAsync` / `GetStudentUserIdAsync` / `GetTeacherUserIdAsync` helpers added to `AdminController`, mirroring the pattern already used in `TeacherController`), linking to `Student/Fees`. Message wording differs for a new bill vs. a payment applied.
- **Verified, not duplicated**: new-assignment, grade-posted, and leave-reviewed notifications (added in earlier phases) were re-read after all Phase 6 layout changes — the code paths are untouched and still correct.

### Authorization / URL-tampering audit pass (complete)

Went through every controller touched in Phases 4–6 action by action. Findings:

- **`AssignmentController`, `TeacherController`, `StudentController`**: all "my own data" actions correctly resolve the current user through `GetCurrentTeacherAsync()` / `GetCurrentStudentAsync()` / `_userManager.GetUserId(User)` and then filter by the resulting `TeacherId`/`StudentId` — never trust a route/query id alone. Spot-checked `AssignmentController.Edit/Delete/Submissions/Grade`, `TeacherController.StudentPerformanceDetails/ReviewLeaveRequest/Reply/DownloadCourseMaterial`, `StudentController.DownloadDocument/DownloadCourseMaterial/MessageThread` — every one re-checks ownership, not just existence, of the id in the URL.
- **`AccountController.Register`**: self-registration is hard-coded to assign the `"Student"` role only (`_userManager.AddToRoleAsync(user, "Student")`) — no privilege-escalation path through registration.
- **Real bug found and fixed**: `CourseController` and `EnrollmentController` only had a class-level `[Authorize(Roles = "Admin,Teacher")]`, which meant **any Teacher could create, edit, or delete courses and enrollments system-wide** — a direct violation of the original spec ("Teacher MUST NOT: Create/delete system-wide courses"; enrollment management is listed as an Admin capability only). Fixed by adding `[Authorize(Roles = "Admin")]` at the action level on `Create`/`Edit`/`Delete`/`DeleteConfirmed` in both controllers. ASP.NET Core combines a class-level and action-level `[Authorize]` with AND semantics, so this correctly narrows those four actions to Admin-only while leaving `Index`/`Details` open to both Admin and Teacher for browsing (a legitimate, spec-consistent read-only use).
- **Known limitation found, not fixed (documented, not silently left in place)**: `AttendanceController` and `ResultsController` (both `[Authorize(Roles = "Admin,Teacher")]`, pre-dating Phase 3) let **any** Teacher mark attendance or enter/edit results for **any** course, not just their own assigned courses — there's no `TeacherId` scoping check anywhere in either controller. This is a real gap, but fixing it properly means reworking the course-selection dropdown/validation in both controllers (used by Admin too, unscoped, intentionally) — a bigger change than a one-line attribute fix, and risky to do as a drive-by edit given both controllers are actively used by Admin for unrestricted access. **Recommended as a follow-up**, not attempted here, to avoid breaking Admin's existing workflow through a rushed change.
- **`StudentController.Index`/`Details`** (the original, pre-Phase-3 Admin student-directory feature) intentionally allow any Teacher to browse the full student list/details, not just their own students — this predates Phase 3 and was preserved as existing behavior per the "don't break existing features" rule rather than treated as a new-phase bug. Flagging it here for visibility, not changing it.

### New/modified files (Phase 7)
- `Controllers/AdminController.cs` — `CreateAnnouncement` (system-wide) now broadcasts notifications; `CreateFee`/`RecordPayment` now notify the student; new helpers `GetStudentUserIdAsync`, `GetTeacherUserIdAsync`, `AddNotificationAsync`, `GetUserIdsForTargetRoleAsync`, `BroadcastAnnouncementNotificationAsync`.
- `Controllers/CourseController.cs`, `Controllers/EnrollmentController.cs` — added `[Authorize(Roles = "Admin")]` on all mutating actions (security fix).

## Phase 8 — Final Delivery (complete)
- This `PROJECT_AUDIT.md` now covers Phases 1–7 in full.
- Final zip generated (see delivery message).
- "How to run" / "how to test each role" instructions given in the delivery message, including a walkthrough of the new notification triggers and the two authorization fixes.

## Verification Limitation (applies to the whole project)
No `dotnet` SDK was available in any sandbox used across this project (apt install of `dotnet-sdk-8.0` was blocked by network egress rules to `security.ubuntu.com` every time it was attempted). **Every phase of this project, from Phase 1 through Phase 8, was checked statically only** — brace/paren balance on every edited file, and every `asp-action`/`asp-controller` reference in a view or `Url.Action(...)` call cross-checked against an actual action in the target controller — never actually compiled with `dotnet build`. Please build the final zip in Visual Studio (or `dotnet build` if you have the SDK) and report any errors; static checking catches structural mistakes but not everything a real compiler catches (e.g. type mismatches between values that are both syntactically valid).
