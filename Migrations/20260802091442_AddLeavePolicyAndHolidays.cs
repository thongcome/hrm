using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeavePolicyAndHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lve_CompanyHoliday",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lve_CompanyHoliday", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lve_LeavePolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    LeaveType = table.Column<int>(type: "int", nullable: false),
                    EntitlementDaysPerYear = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lve_LeavePolicy", x => x.Id);
                });

            // Thai Labour Protection Act statutory minimums for company 001 —
            // HR should review/adjust via the LeavePolicyAdmin page (e.g. many
            // companies grant more than the legal minimum). Sick=30 is the
            // legally-mandated PAID sick day cap per year, not a hard limit on
            // how many days someone may be sick. Ordination/Other default to 0
            // (not legally mandated) since guessing a number would be worse
            // than an explicit "not yet configured" state.
            migrationBuilder.InsertData(
                table: "Lve_LeavePolicy",
                columns: new[] { "Id", "CompanyId", "LeaveType", "EntitlementDaysPerYear", "IsActive" },
                values: new object[,]
                {
                    { 1, "001", 0, 30m, true },  // Sick
                    { 2, "001", 1, 3m, true },   // Personal
                    { 3, "001", 2, 6m, true },   // Vacation
                    { 4, "001", 3, 98m, true },  // Maternity
                    { 5, "001", 4, 0m, true },   // Ordination
                    { 6, "001", 9, 0m, true },   // Other
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lve_CompanyHoliday");

            migrationBuilder.DropTable(
                name: "Lve_LeavePolicy");
        }
    }
}
