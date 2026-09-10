# Student Management System

A web-based **Student Management System** developed using **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**. The system is designed to manage student information and academic activities through a centralized and user-friendly platform.

## Technologies Used

* ASP.NET Core MVC
* C#
* Entity Framework Core
* SQL Server
* Razor Views
* HTML5
* CSS3
* Bootstrap
* Git & GitHub
* Visual Studio

## Main Modules

The system is divided into the following modules:

1. Student Registration
2. Student Profile
3. Department Management
4. Course Management
5. Enrollment Management
6. Attendance Management
7. Result Management
8. Student Search
9. Dashboard

## Student Module

The Student Module is responsible for managing student registration and student information.

### Features

* Add new students
* View all registered students
* View individual student profiles
* Edit student information
* Delete students
* Delete confirmation
* Search students by:

  * Name
  * Email
  * Phone number
  * Student ID
* Form validation
* Nepali mobile number validation
* Email format validation
* Date of birth validation
* Duplicate email prevention
* Student dashboard
* Recent students display
* Student navigation

### Student Information

The Student entity currently contains:

* Student ID
* Full Name
* Email Address
* Phone Number
* Address
* Gender
* Date of Birth
* Department ID

> Department integration will be completed after the Department module is integrated with the Student module.

## Database

The project uses **SQL Server** with **Entity Framework Core**.

Main database entities include:

* Students
* Departments
* Courses
* Enrollments
* Attendance
* Results

## Project Structure

```text
Student_Management_System
│
├── StudentManagementSystem
│   ├── Controllers
│   ├── Data
│   ├── Models
│   ├── Views
│   ├── wwwroot
│   ├── Program.cs
│   └── appsettings.json
│
├── .gitignore
└── README.md
```

## Student Module Structure

```text
Controllers
└── StudentController.cs

Models
├── Student.cs
└── DashboardViewModel.cs

Data
└── ApplicationDbContext.cs

Views
├── Home
│   └── Index.cshtml
│
├── Student
│   ├── Index.cshtml
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   ├── Details.cshtml
│   └── Delete.cshtml
│
└── Shared
    └── _Layout.cshtml
```

## Git Branch Structure

Each team member works on a separate branch.

```text
master
│
├── aakriti-911
├── member2-department-course
├── member3-enrollment-attendance
└── member4-results
```

### Member 1 — Student Module

The aakriti-911 branch contains:

* Student Registration
* Student CRUD
* Student Profile
* Student Search
* Student Validation
* Dashboard
* Navigation
* Student UI

Other branches contain the remaining modules and will be integrated into the `master` branch after development and testing.

## How to Run the Project

### 1. Clone the repository

```bash
git clone <repository-url>
```

### 2. Open the project

Open the `.sln` file in Visual Studio.

### 3. Configure the database

Update the SQL Server connection string in:

```text
appsettings.json
```

### 4. Apply migrations

Open **Package Manager Console** in Visual Studio and run:

```powershell
Update-Database
```

### 5. Run the application

Press:

```text
Ctrl + F5
```

or:

```text
F5
```

## Project Status

### Completed

* Student Registration
* Student CRUD
* Student Profile
* Student Search
* Student Validation
* Dashboard
* Navigation
* SQL Server & EF Core setup

### In Progress

* Department Management
* Course Management
* Enrollment
* Attendance
* Results
* Final module integration

## Future Integration

After all modules are completed, the team will:

* Integrate all branches
* Establish database relationships
* Perform final migrations
* Conduct system-wide testing
* Apply a consistent final UI design
* Fix integration issues
* Prepare project documentation
* Prepare the final demonstration

## Team Project

**Student Management System**

Developed as an academic project using ASP.NET Core MVC and Entity Framework Core.
