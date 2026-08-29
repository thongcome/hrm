using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveModuleUpgradeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AdhocPayItemId",
                table: "Lve_LeaveRequest",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HalfDayPeriod",
                table: "Lve_LeaveRequest",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalfDay",
                table: "Lve_LeaveRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "MedCertDocCenterId",
                table: "Lve_LeaveRequest",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarryOverMode",
                table: "Lve_LeavePolicy",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // defaultValue: true — every existing leave-type policy row is
            // currently a paid leave type (no prior column to derive this
            // from). Backfilling to false would make every leave type in
            // every company suddenly eligible for the unpaid-leave payroll
            // deduction the moment this migration runs.
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Lve_LeavePolicy",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxCarryOverDays",
                table: "Lve_LeavePolicy",
                type: "decimal(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkDaysMask",
                table: "Lve_CompanySetting",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdhocPayItemId",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropColumn(
                name: "HalfDayPeriod",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropColumn(
                name: "IsHalfDay",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropColumn(
                name: "MedCertDocCenterId",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropColumn(
                name: "CarryOverMode",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropColumn(
                name: "MaxCarryOverDays",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropColumn(
                name: "WorkDaysMask",
                table: "Lve_CompanySetting");
        }
    }
}
