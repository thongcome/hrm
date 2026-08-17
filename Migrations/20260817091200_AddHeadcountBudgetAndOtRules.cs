using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddHeadcountBudgetAndOtRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BlockOverBudgetCreation",
                table: "Pay_PayslipSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Att_OtRule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    DayType = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_OtRule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pos_HeadcountBudget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedCount = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_HeadcountBudget", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                column: "BlockOverBudgetCreation",
                value: false);

            // Seed Section 61-63 legal-minimum OT multipliers for every
            // company present in HREMPLOYEE today (verified via live query:
            // only "001" exists in this DB) — same "seed Thai legal-minimum
            // defaults" precedent as the leave/holiday policy migration.
            // HR can adjust per company's own OT policy afterwards at
            // /att/ot-rules.
            migrationBuilder.InsertData(
                table: "Att_OtRule",
                columns: new[] { "Id", "CompanyId", "DayType", "Multiplier", "IsActive" },
                values: new object[,]
                {
                    { 1L, "001", 1, 1.5m, true },  // Workday
                    { 2L, "001", 2, 3.0m, true },  // Holiday
                    { 3L, "001", 3, 3.0m, true },  // RestDay
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Att_OtRule");

            migrationBuilder.DropTable(
                name: "Pos_HeadcountBudget");

            migrationBuilder.DropColumn(
                name: "BlockOverBudgetCreation",
                table: "Pay_PayslipSettings");
        }
    }
}
