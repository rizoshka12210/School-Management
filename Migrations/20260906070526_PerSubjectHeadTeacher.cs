using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class PerSubjectHeadTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeadTeacher",
                table: "Teachers");

            migrationBuilder.AddColumn<int>(
                name: "HeadTeacherId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_HeadTeacherId",
                table: "Subjects",
                column: "HeadTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Teachers_HeadTeacherId",
                table: "Subjects",
                column: "HeadTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Teachers_HeadTeacherId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_HeadTeacherId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "HeadTeacherId",
                table: "Subjects");

            migrationBuilder.AddColumn<bool>(
                name: "IsHeadTeacher",
                table: "Teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
