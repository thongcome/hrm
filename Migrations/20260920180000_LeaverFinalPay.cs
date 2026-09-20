using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // Leaver final pay (CEO, 19 ก.ย. 2569 — ม.70: wages owed on termination within 3 days) — พอร์ตจาก Advance.Payroll:
    // the member list of a FinalPay run, the two earnings a leaver's settlement needs besides severance
    // (pay in lieu of notice ม.17/1, unused annual leave ม.67 — both taxable ม.40(1)), and the run index
    // that must allow several leaver runs a month (RunType 4). Raw SQL only, so the Designer can stay a stub.
    public partial class LeaverFinalPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PayrollRunMember",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Pay_PayrollRunMember", x => x.Id));

            migrationBuilder.CreateIndex(name: "UX_Pay_PayrollRunMember_Run_Emp", table: "Pay_PayrollRunMember",
                columns: new[] { "PayrollRunId", "HremployeeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Pay_PayrollRunMember_Emp", table: "Pay_PayrollRunMember", column: "HremployeeId");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code = 'NOTICE_PAY')
    INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsActive, SortOrder, IsTaxable, IsSsoWageBase, IsProrated, IsProvidentFundWageBase)
    VALUES ('NOTICE_PAY', N'สินจ้างแทนการบอกกล่าวล่วงหน้า', N'Pay in lieu of notice', 0, 1, 1, '5040-SEVERANCE', 1, 20, 1, 0, 0, 0);
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code = 'LEAVE_PAYOUT')
    INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsActive, SortOrder, IsTaxable, IsSsoWageBase, IsProrated, IsProvidentFundWageBase)
    VALUES ('LEAVE_PAYOUT', N'ค่าจ้างวันหยุดพักผ่อนประจำปีที่ไม่ได้ใช้', N'Unused annual leave payout', 0, 1, 1, '5000-SALARY', 1, 21, 1, 0, 0, 0);

DROP INDEX IF EXISTS [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [Pay_PayrollRun];
CREATE UNIQUE INDEX [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [Pay_PayrollRun] ([CompanyId], [PayrollPeriod], [TermNo], [RunType])
    WHERE [Status] <> 9 AND [RunType] <> 4;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Pay_PayItemType SET IsActive = 0 WHERE Code IN ('NOTICE_PAY', 'LEAVE_PAYOUT');
DROP INDEX IF EXISTS [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [Pay_PayrollRun];
CREATE UNIQUE INDEX [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [Pay_PayrollRun] ([CompanyId], [PayrollPeriod], [TermNo], [RunType])
    WHERE [Status] <> 9;");
            migrationBuilder.DropTable(name: "Pay_PayrollRunMember");
        }
    }
}
