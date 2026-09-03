using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBigExam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBigExamGrader",
                table: "Teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BigExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigExams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BigExamGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigExamId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    TeacherId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigExamGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BigExamGrades_BigExams_BigExamId",
                        column: x => x.BigExamId,
                        principalTable: "BigExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BigExamGrades_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BigExamGrades_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BigExamGrades_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_BigExamId_StudentId",
                table: "BigExamGrades",
                columns: new[] { "BigExamId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_GroupId",
                table: "BigExamGrades",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_StudentId",
                table: "BigExamGrades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_BigExamGrades_TeacherId",
                table: "BigExamGrades",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BigExamGrades");

            migrationBuilder.DropTable(
                name: "BigExams");

            migrationBuilder.DropColumn(
                name: "IsBigExamGrader",
                table: "Teachers");
        }
    }
}
