using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PayrollPeriod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TermNo = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollPeriod", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Pay_PayrollPeriod",
                columns: new[] { "Id", "CompanyId", "IsActive", "Label", "Month", "PeriodEnd", "PeriodStart", "TermNo", "Year" },
                values: new object[,]
                {
                    { 1L, "001", true, "ก.ค. 2569 งวดที่ 1", 7, new DateOnly(2026, 7, 31), new DateOnly(2026, 7, 1), 1, 2026 },
                    { 2L, "001", true, "ส.ค. 2569 งวดที่ 1", 8, new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1), 1, 2026 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_PayrollPeriod");
        }
    }
}
