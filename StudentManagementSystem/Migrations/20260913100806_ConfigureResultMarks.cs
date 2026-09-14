using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureResultMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results");

            migrationBuilder.AlterColumn<decimal>(
                name: "Marks",
                table: "Results",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results",
                columns: new[] { "StudentId", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results");

            migrationBuilder.AlterColumn<decimal>(
                name: "Marks",
                table: "Results",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Results_StudentId_CourseId",
                table: "Results",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);
        }
    }
}
