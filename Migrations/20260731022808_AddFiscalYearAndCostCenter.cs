using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalYearAndCostCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiscalYearStartMonth",
                table: "Pay_PayslipSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                table: "Pay_PayrollEmployee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                table: "HREMPLOYEE",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                table: "com_organization",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                column: "FiscalYearStartMonth",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalYearStartMonth",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                table: "Pay_PayrollEmployee");

            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                table: "com_organization");
        }
    }
}
