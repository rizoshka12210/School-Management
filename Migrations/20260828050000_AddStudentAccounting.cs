using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SchoolManagementSystem.Web.Data;

#nullable disable

namespace SchoolManagementSystem.Web.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828050000_AddStudentAccounting")]
public partial class AddStudentAccounting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StudentPayments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StudentId = table.Column<int>(type: "integer", nullable: false),
                Year = table.Column<int>(type: "integer", nullable: false),
                Month = table.Column<int>(type: "integer", nullable: false),
                ExpectedAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                PaidAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Note = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudentPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_StudentPayments_Students_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Students",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StudentPayments_StudentId_Year_Month",
            table: "StudentPayments",
            columns: new[] { "StudentId", "Year", "Month" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StudentPayments");
    }
}
