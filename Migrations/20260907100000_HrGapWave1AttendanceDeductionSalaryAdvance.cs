using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>
    /// HR gap wave 1 (customer spec REQ-092/100/101/104):
    ///  - Att_DailyAttendance.LateMinutes / EarlyLeaveMinutes (aggregation now records how late)
    ///  - Att_CompanySetting.DetectAbsence (opt-in automatic absence rows)
    ///  - Pay_AttendanceDeductionPolicy (late / absence / daily-wage rules per company)
    ///  - Pay_SalaryAdvance (advance recovered in full from the target period's payroll)
    ///  - pay items LATE / ABSENT / SAL_ADVANCE + two menu rows
    /// Hand-authored like PayrollPhaseABAndPayElementCatalog (design-time build blocked by
    /// the running dev instance); already applied and stamped on the dev DB.
    /// </summary>
    public partial class HrGapWave1AttendanceDeductionSalaryAdvance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "LateMinutes", table: "Att_DailyAttendance", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "EarlyLeaveMinutes", table: "Att_DailyAttendance", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(name: "DetectAbsence", table: "Att_CompanySetting", type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Pay_AttendanceDeductionPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    LateMode = table.Column<int>(type: "int", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    LateAmountPerMinute = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    LateAmountPerOccurrence = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AbsentMode = table.Column<int>(type: "int", nullable: false),
                    DaysPerMonthDivisor = table.Column<int>(type: "int", nullable: false),
                    HoursPerDay = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    DailyWageMode = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Pay_AttendanceDeductionPolicy", x => x.Id); });
            migrationBuilder.CreateIndex(name: "IX_Pay_AttendanceDeductionPolicy_CompanyId", table: "Pay_AttendanceDeductionPolicy", column: "CompanyId", unique: true);

            migrationBuilder.CreateTable(
                name: "Pay_SalaryAdvance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdvanceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TargetPeriod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedByPayrollRunId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Pay_SalaryAdvance", x => x.Id); });
            migrationBuilder.CreateIndex(name: "IX_Pay_SalaryAdvance_HremployeeId_TargetPeriod", table: "Pay_SalaryAdvance", columns: new[] { "HremployeeId", "TargetPeriod" });

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code='LATE') INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsActive, SortOrder, IsTaxable, IsSsoWageBase, IsProrated) VALUES ('LATE', N'หักมาสาย', 'Late Deduction', 1, -1, 1, NULL, 1, 91, 0, 0, 0);
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code='ABSENT') INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsActive, SortOrder, IsTaxable, IsSsoWageBase, IsProrated) VALUES ('ABSENT', N'หักขาดงาน', 'Absence Deduction', 1, -1, 1, NULL, 1, 92, 0, 0, 0);
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code='SAL_ADVANCE') INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsActive, SortOrder, IsTaxable, IsSsoWageBase, IsProrated) VALUES ('SAL_ADVANCE', N'หักคืนเงินเบิกล่วงหน้า', 'Salary Advance Recovery', 1, -1, 1, '1310-ADV-RECV', 1, 93, 0, 0, 0);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/admin/attendance-deduction')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'นโยบายหักสาย/ขาดงาน และค่าจ้างรายวัน', 'Attendance Deduction Policy', menulevel, isfinal, 166, menucode, isshow, '/pay/admin/attendance-deduction', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/admin/tax-deduction-config';
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/salary-advances')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'เบิกเงินเดือนล่วงหน้า', 'Salary Advances', menulevel, isfinal, 85, menucode, isshow, '/pay/salary-advances', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/recurring-items';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) IN ('/pay/admin/attendance-deduction','/pay/salary-advances');");
            migrationBuilder.DropTable(name: "Pay_SalaryAdvance");
            migrationBuilder.DropTable(name: "Pay_AttendanceDeductionPolicy");
            migrationBuilder.DropColumn(name: "DetectAbsence", table: "Att_CompanySetting");
            migrationBuilder.DropColumn(name: "EarlyLeaveMinutes", table: "Att_DailyAttendance");
            migrationBuilder.DropColumn(name: "LateMinutes", table: "Att_DailyAttendance");
        }
    }
}
