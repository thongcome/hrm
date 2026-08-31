using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdvanceNoticeDays",
                table: "Lve_LeaveType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowRetroactive",
                table: "Lve_LeaveType",
                type: "bit",
                nullable: false,
                defaultValue: true); // matches the model default — most types allow back-dated requests

            migrationBuilder.AddColumn<string>(
                name: "AttachmentDocName",
                table: "Lve_LeaveType",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AttachmentMinDays",
                table: "Lve_LeaveType",
                type: "decimal(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DayCountMethod",
                table: "Lve_LeaveType",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntitlementFrequency",
                table: "Lve_LeaveType",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MustBeConsecutive",
                table: "Lve_LeaveType",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // CEO-approved Thai-practice rule defaults for the 14 seeded leave
            // types (BA table, 31 ส.ค. 2569) — run exactly once here so later
            // HR edits via /leave-requests/leave-types are never overwritten.
            // Source of truth: Services/Leave/LeaveTypeRuleDefaults.sql.txt.
            migrationBuilder.Sql(@"
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 1, DayCountMethod = 0, EntitlementFrequency = 0, AttachmentDocName = N'ใบรับรองแพทย์', AttachmentMinDays = 3.0 WHERE Code = 'Sick';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 1, DayCountMethod = 0, EntitlementFrequency = 0, AdvanceNoticeDays = 1 WHERE Code = 'Personal';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 0, DayCountMethod = 0, EntitlementFrequency = 0, AdvanceNoticeDays = 3 WHERE Code = 'Vacation';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 1, EntitlementFrequency = 1, ApplicableGender = 'F', AttachmentDocName = N'ใบรับรองแพทย์' WHERE Code = 'Maternity';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, EntitlementFrequency = 1, AttachmentDocName = N'ใบรับรองแพทย์' WHERE Code = 'Sterilization';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 1, EntitlementFrequency = 1, ApplicableGender = 'M', AttachmentDocName = N'หมายเรียก' WHERE Code = 'Military';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 0, DayCountMethod = 0, EntitlementFrequency = 0, AdvanceNoticeDays = 7 WHERE Code = 'Training';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 1, EntitlementFrequency = 2, ApplicableGender = 'M', AdvanceNoticeDays = 15, AttachmentDocName = N'ใบฎีกา/หนังสือรับรองการอุปสมบท' WHERE Code = 'Ordination';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 1, EntitlementFrequency = 2, AdvanceNoticeDays = 30 WHERE Code = 'Hajj';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 0, EntitlementFrequency = 1, ApplicableGender = 'M' WHERE Code = 'Paternity';
UPDATE Lve_LeaveType SET AllowHalfDay = 0, MustBeConsecutive = 1, AllowRetroactive = 0, DayCountMethod = 1, EntitlementFrequency = 2, AdvanceNoticeDays = 7 WHERE Code = 'Marriage';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 1, DayCountMethod = 0, EntitlementFrequency = 1 WHERE Code = 'Bereavement';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 0, DayCountMethod = 0, EntitlementFrequency = 0, AdvanceNoticeDays = 7 WHERE Code = 'SiteVisit';
UPDATE Lve_LeaveType SET AllowHalfDay = 1, AllowRetroactive = 1, DayCountMethod = 0, EntitlementFrequency = 0 WHERE Code = 'Other';
");

            // Repair the two Pay_AdhocPayItem rows written with Buddhist-year
            // period codes before the InvariantCulture fix (see commit
            // f25e31c): 256908 = ส.ค. 2026, 052569 = MMyyyy of พ.ค. 2026.
            migrationBuilder.Sql(@"
UPDATE Pay_AdhocPayItem SET TargetPeriod = '202608' WHERE TargetPeriod = '256908';
UPDATE Pay_AdhocPayItem SET TargetPeriod = '202605' WHERE TargetPeriod = '052569';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceNoticeDays",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "AllowRetroactive",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "AttachmentDocName",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "AttachmentMinDays",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "DayCountMethod",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "EntitlementFrequency",
                table: "Lve_LeaveType");

            migrationBuilder.DropColumn(
                name: "MustBeConsecutive",
                table: "Lve_LeaveType");
        }
    }
}
