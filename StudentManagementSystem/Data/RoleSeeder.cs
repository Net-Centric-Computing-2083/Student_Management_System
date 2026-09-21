using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Models;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var context =
                serviceProvider.GetRequiredService<ApplicationDbContext>();


            // =========================================================
            // 1. CREATE ROLES
            // =========================================================

            string[] roles =
            {
                "Admin",
                "Teacher",
                "Student"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult =
                        await roleManager.CreateAsync(
                            new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine(
                                $"Role creation error: {error.Description}");
                        }
                    }
                }
            }


            // =========================================================
            // 2. SEED SEMESTER RECORDS
            // =========================================================

            if (!await context.Semesters.AnyAsync())
            {
                context.Semesters.AddRange(
                    new Semester
                    {
                        AcademicYear = "2026/2027",
                        SemesterName = "First Semester"
                    },

                    new Semester
                    {
                        AcademicYear = "2026/2027",
                        SemesterName = "Second Semester"
                    }
                );

                await context.SaveChangesAsync();
            }


            // =========================================================
            // 3. CREATE ADMIN ACCOUNT
            // =========================================================

            const string adminEmail =
                "admin@studentmanagement.com";

            const string adminPassword =
                "Admin@123";

            var admin =
                await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result =
                    await userManager.CreateAsync(
                        admin,
                        adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        admin,
                        "Admin");

                    Console.WriteLine(
                        "Admin account created successfully.");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(
                            $"Admin creation error: {error.Description}");
                    }
                }
            }
            else
            {
                // Make sure the existing admin has the Admin role
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    await userManager.AddToRoleAsync(
                        admin,
                        "Admin");
                }
            }


            // =========================================================
            // 4. CREATE TEACHER ACCOUNT
            // =========================================================

            const string teacherEmail =
                "teacher@studentmanagement.com";

            const string teacherPassword =
                "Teacher@123";

            var teacher =
                await userManager.FindByEmailAsync(teacherEmail);

            if (teacher == null)
            {
                teacher = new ApplicationUser
                {
                    UserName = teacherEmail,
                    Email = teacherEmail,
                    FullName = "System Teacher",
                    EmailConfirmed = true
                };

                var result =
                    await userManager.CreateAsync(
                        teacher,
                        teacherPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        teacher,
                        "Teacher");

                    Console.WriteLine(
                        "Teacher account created successfully.");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(
                            $"Teacher creation error: {error.Description}");
                    }
                }
            }
            else
            {
                // Make sure the existing teacher has the Teacher role
                if (!await userManager.IsInRoleAsync(
                        teacher,
                        "Teacher"))
                {
                    await userManager.AddToRoleAsync(
                        teacher,
                        "Teacher");
                }
            }
        }
    }
}