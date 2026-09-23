**Student Management System**

A role-based web application for managing students, teachers, courses, attendance, results, assignments, fees, and communication in an academic institution — built with ASP.NET Core MVC, Entity Framework Core, and SQL Server.

**Features**

Admin — manages Departments, Courses, Teachers, Students, Semesters, and Class Schedules; posts system-wide/course announcements; handles Fees & Payments; uploads Documents; sends Notifications; reviews Leave Requests; views dashboard stats/reports; manages Users & Roles.

Teacher — views assigned courses and students; marks Attendance; enters Results (auto-calculated grades); creates and grades Assignments; posts course announcements/materials; reviews student leave requests; messages students.

Student — personal dashboard (GPA, attendance %, pending assignments); views Courses, Class Schedule, Results, Attendance, Fees; submits Assignments and views grades; downloads Documents; submits Leave Requests; contacts Teachers; views Announcements/Notifications.

Cross-cutting — role-based auth via ASP.NET Core Identity (Admin/Teacher/Student); automatic notifications on key events (new assignment, grade posted, fee billed, etc.); search/filter/sort/pagination on major lists; anti-forgery protection; users can only access their own records (no ID tampering via URL).

**Tech Stack**
Framework: ASP.NET Core MVC (.NET 10)
ORM: Entity Framework Core 10
Database: SQL Server
Auth: ASP.NET Core Identity
Frontend: Razor Views, Bootstrap, jQuery


**Project Structure**
StudentManagementSystem/
├── Controllers/     # MVC controllers
├── Models/          # EF Core entity models
├── ViewModels/       # View-specific models
├── Views/            # Razor views
├── Data/             # DbContext and RoleSeeder
├── Migrations/       # EF Core migrations
├── wwwroot/          # CSS, JS, uploaded files
├── Program.cs        # App startup
└── appsettings.json   # Configuration

**Getting Started**

Prerequisites
.NET 10 SDK
SQL Server (LocalDB, Express, or full instance)
Visual Studio 2022+ or the dotnet CLI

Setup
1. Clone the repo:
bash
   git clone <your-repo-url>
   cd StudentManagementSystem
2. Update the connection string in appsettings.json:
json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=StudentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
3. Build and run (migrations apply automatically on startup):
bash
   dotnet build
   dotnet run

Or open StudentManagementSystem.slnx in Visual Studio and press F5.
Roles, default semesters, and a seeded Admin account are created automatically on first run.

**Default Admin Account**

Email	admin@studentmanagement.com
Password	Admin@123


**Testing Other Roles**

Student: an Admin/Teacher must first create a Student profile. The student then self-registers using the same email, registration number, and date of birth as that profile.
Teacher: created/assigned through the Admin user-management flow — no fixed seeded account exists.

**Known Limitations**

AttendanceController and ResultsController let any Teacher mark attendance/enter results for any course, not just their own.
Run dotnet build and resolve any errors before deploying — the project has only been statically reviewed.
