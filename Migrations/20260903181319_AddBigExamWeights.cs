using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBigExamWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Score",
                table: "BigExamGrades",
                newName: "RawScore");

            migrationBuilder.AddColumn<decimal>(
                name: "BigExamMaxRawScore",
                table: "Subjects",
                type: "numeric",
                nullable: false,
                defaultValue: 100m);

            migrationBuilder.AddColumn<decimal>(
                name: "BigExamMaxWeightedScore",
                table: "Subjects",
                type: "numeric",
                nullable: false,
                defaultValue: 100m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BigExamMaxRawScore",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "BigExamMaxWeightedScore",
                table: "Subjects");

            migrationBuilder.RenameColumn(
                name: "RawScore",
                table: "BigExamGrades",
                newName: "Score");
        }
    }
}
