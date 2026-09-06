using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>
    /// Payroll Phase A/B + Pay Element catalog + immutable posted runs.
    /// Hand-authored (the design-time build cannot run while the dev instance
    /// holds bin\Debug) and applied out-of-band to the dev DB, which is why it
    /// only ADDs columns with defaults / a new table — nothing here rewrites
    /// existing data. On a fresh database `dotnet ef database update` runs it
    /// normally.
    ///  - Pay_PayrollRun: IsCalculating/CalcStartedAt/CalcError (background calc job)
    ///  - Pay_PayslipSettings: PayDayOfMonth/PayDateAdjustBackward (default pay date)
    ///  - Pay_PayItemType: IsTaxable/IsSsoWageBase/IsProrated (engine reads flags)
    ///  - Pay_PayrollRunHold (per-run employee holds with reason)
    ///  - triggers refusing UPDATE/DELETE on rows of Posted/Paid runs
    ///  - sc_menu row for /pay/admin/pay-item-types (id from MAX per CLAUDE.md)
    /// </summary>
    public partial class PayrollPhaseABAndPayElementCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "IsCalculating", table: "Pay_PayrollRun", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<DateTime>(name: "CalcStartedAt", table: "Pay_PayrollRun", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CalcError", table: "Pay_PayrollRun", type: "nvarchar(1000)", maxLength: 1000, nullable: true);

            migrationBuilder.AddColumn<int>(name: "PayDayOfMonth", table: "Pay_PayslipSettings", type: "int", nullable: false, defaultValue: 28);
            migrationBuilder.AddColumn<bool>(name: "PayDateAdjustBackward", table: "Pay_PayslipSettings", type: "bit", nullable: false, defaultValue: true);

            migrationBuilder.AddColumn<bool>(name: "IsTaxable", table: "Pay_PayItemType", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "IsSsoWageBase", table: "Pay_PayItemType", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "IsProrated", table: "Pay_PayItemType", type: "bit", nullable: false, defaultValue: false);

            // Thai defaults: wages (base + regular allowances) form the SSO wage base;
            // OT / one-off items do not. Deductions and informational rows are not taxable income.
            migrationBuilder.Sql("UPDATE Pay_PayItemType SET IsSsoWageBase = 1 WHERE Code IN ('BASE','ALLOWANCE');");
            migrationBuilder.Sql("UPDATE Pay_PayItemType SET IsProrated = 1 WHERE Code = 'BASE';");
            migrationBuilder.Sql("UPDATE Pay_PayItemType SET IsTaxable = 0 WHERE Code IN ('REIMBURSEMENT','ADJUST') OR Category = 1;");

            migrationBuilder.CreateTable(
                name: "Pay_PayrollRunHold",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeldByUserId = table.Column<long>(type: "bigint", nullable: false),
                    HeldDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ReleasedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReleasedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollRunHold", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollRunHold_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollRunHold_PayrollRunId",
                table: "Pay_PayrollRunHold",
                column: "PayrollRunId");

            // Immutable posted runs (document principle): once Posted (4) / Paid (5),
            // employee rows and line items cannot be changed by any code path.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_PayrollEmployee_Immutable ON Pay_PayrollEmployee AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d JOIN Pay_PayrollRun r ON r.Id = d.PayrollRunId WHERE r.Status IN (4, 5))
    BEGIN
        RAISERROR (N'Payroll run is Posted/Paid — its employee rows are immutable. Create a reversal or adjustment run instead.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_PayrollLineItem_Immutable ON Pay_PayrollLineItem AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d JOIN Pay_PayrollEmployee e ON e.Id = d.PayrollEmployeeId JOIN Pay_PayrollRun r ON r.Id = e.PayrollRunId WHERE r.Status IN (4, 5))
    BEGIN
        RAISERROR (N'Payroll run is Posted/Paid — its line items are immutable. Create a reversal or adjustment run instead.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END");

            // Menu row for the Pay Element catalog page — sibling of tax-deduction-config,
            // same menucode (PAY_ADMIN) so existing grants cover it; identity ids, no hardcoding.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/admin/pay-item-types')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'ประเภทรายการเงินได้-เงินหัก (Pay Elements)', 'Pay Element Catalog', menulevel, isfinal, 165, menucode, isshow, '/pay/admin/pay-item-types', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/admin/tax-deduction-config';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/pay/admin/pay-item-types';");
            migrationBuilder.Sql("IF OBJECT_ID('trg_PayrollLineItem_Immutable', 'TR') IS NOT NULL DROP TRIGGER trg_PayrollLineItem_Immutable;");
            migrationBuilder.Sql("IF OBJECT_ID('trg_PayrollEmployee_Immutable', 'TR') IS NOT NULL DROP TRIGGER trg_PayrollEmployee_Immutable;");
            migrationBuilder.DropTable(name: "Pay_PayrollRunHold");
            migrationBuilder.DropColumn(name: "IsProrated", table: "Pay_PayItemType");
            migrationBuilder.DropColumn(name: "IsSsoWageBase", table: "Pay_PayItemType");
            migrationBuilder.DropColumn(name: "IsTaxable", table: "Pay_PayItemType");
            migrationBuilder.DropColumn(name: "PayDateAdjustBackward", table: "Pay_PayslipSettings");
            migrationBuilder.DropColumn(name: "PayDayOfMonth", table: "Pay_PayslipSettings");
            migrationBuilder.DropColumn(name: "CalcError", table: "Pay_PayrollRun");
            migrationBuilder.DropColumn(name: "CalcStartedAt", table: "Pay_PayrollRun");
            migrationBuilder.DropColumn(name: "IsCalculating", table: "Pay_PayrollRun");
        }
    }
}
