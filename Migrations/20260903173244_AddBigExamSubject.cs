using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBigExamSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BigExamGrades_BigExamId_StudentId",
                table: "BigExamGrades");

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "BigExamGrades",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_BigExamId_StudentId_SubjectId",
                table: "BigExamGrades",
                columns: new[] { "BigExamId", "StudentId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_SubjectId",
                table: "BigExamGrades",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_BigExamGrades_Subjects_SubjectId",
                table: "BigExamGrades",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BigExamGrades_Subjects_SubjectId",
                table: "BigExamGrades");

            migrationBuilder.DropIndex(
                name: "IX_BigExamGrades_BigExamId_StudentId_SubjectId",
                table: "BigExamGrades");

            migrationBuilder.DropIndex(
                name: "IX_BigExamGrades_SubjectId",
                table: "BigExamGrades");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "BigExamGrades");

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_BigExamId_StudentId",
                table: "BigExamGrades",
                columns: new[] { "BigExamId", "StudentId" });
        }
    }
}
