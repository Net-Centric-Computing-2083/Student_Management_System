using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentManagementDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results");

            migrationBuilder.RenameColumn(
                name: "Marks",
                table: "Results",
                newName: "TotalMarks");

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentYear",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalMarks",
                table: "Results",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InternalMarks",
                table: "Results",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PracticalMarks",
                table: "Results",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HodName",
                table: "Departments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    ActivityLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.ActivityLogId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SemesterName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                    table.ForeignKey(
                        name: "FK_Teachers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_Email",
                table: "Students",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_RegistrationNumber",
                table: "Students",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SemesterId",
                table: "Enrollments",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SemesterId",
                table: "Attendances",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_DepartmentId",
                table: "Teachers",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Semesters_SemesterId",
                table: "Attendances",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "SemesterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Semesters_SemesterId",
                table: "Enrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "SemesterId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Semesters_SemesterId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Semesters_SemesterId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_Email",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_RegistrationNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SemesterId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SemesterId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "EnrollmentYear",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FinalMarks",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "InternalMarks",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "PracticalMarks",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HodName",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Attendances");

            migrationBuilder.RenameColumn(
                name: "TotalMarks",
                table: "Results",
                newName: "Marks");

            migrationBuilder.CreateIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results",
                columns: new[] { "StudentId", "CourseId" });
        }
    }
}
