using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SalaryGradeId",
                table: "HREMPLOYEE",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pay_SalaryGrade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    GradeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    MidSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    MaxSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_SalaryGrade", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_SalaryGrade");

            migrationBuilder.DropColumn(
                name: "SalaryGradeId",
                table: "HREMPLOYEE");
        }
    }
}
