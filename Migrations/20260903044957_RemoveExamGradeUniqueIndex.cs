using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExamGradeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamGrades_StudentId_SubjectId",
                table: "ExamGrades");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGrades_StudentId_SubjectId",
                table: "ExamGrades",
                columns: new[] { "StudentId", "SubjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamGrades_StudentId_SubjectId",
                table: "ExamGrades");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGrades_StudentId_SubjectId",
                table: "ExamGrades",
                columns: new[] { "StudentId", "SubjectId" },
                unique: true);
        }
    }
}
