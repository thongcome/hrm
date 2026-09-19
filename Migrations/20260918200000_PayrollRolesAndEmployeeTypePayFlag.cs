using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // CEO, 18 ก.ย. 2569:
    //  1. Pos_EmployeeType.IsPaidByPayroll — "รับเงินเดือนผ่านระบบ" per employee type, so a type
    //     such as กรรมการ can be kept off payroll with a stated reason (PayrollEligibility).
    //  2. Two payroll roles for PayrollStepPermission — เจ้าหน้าที่เงินเดือน (PAYROLL_OFFICER)
    //     and ผู้อนุมัติเงินเดือน (PAYROLL_APPROVER) — with their menu grants and per-page rights.
    //     Per-page rows are copied from the Admin role's seeded paths so they exist before
    //     ProgramRoleService.SeedAsync adds read-only rows for every other page (it never
    //     touches existing rows). Idempotent: every insert is guarded by NOT EXISTS.
    public partial class PayrollRolesAndEmployeeTypePayFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaidByPayroll",
                table: "Pos_EmployeeType",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(@"
DECLARE @companyId bigint = (SELECT TOP 1 company_id FROM sc_role WHERE name = N'Admin' ORDER BY roleid);
IF @companyId IS NULL SET @companyId = 1;

IF NOT EXISTS (SELECT 1 FROM sc_role WHERE rolecode = 'PAYROLL_OFFICER')
    INSERT INTO sc_role (company_id, name, abbr, rolelevel, rolecode, isactive, isHeader, moddate, modby)
    VALUES (@companyId, N'เจ้าหน้าที่เงินเดือน', 'PAYOFF', 1, 'PAYROLL_OFFICER', 1, 0, GETDATE(), 'migration');
IF NOT EXISTS (SELECT 1 FROM sc_role WHERE rolecode = 'PAYROLL_APPROVER')
    INSERT INTO sc_role (company_id, name, abbr, rolelevel, rolecode, isactive, isHeader, moddate, modby)
    VALUES (@companyId, N'ผู้อนุมัติเงินเดือน', 'PAYAPP', 1, 'PAYROLL_APPROVER', 1, 0, GETDATE(), 'migration');

DECLARE @officer bigint = (SELECT roleid FROM sc_role WHERE rolecode = 'PAYROLL_OFFICER');
DECLARE @approver bigint = (SELECT roleid FROM sc_role WHERE rolecode = 'PAYROLL_APPROVER');
DECLARE @admin bigint = (SELECT TOP 1 roleid FROM sc_role WHERE name = N'Admin' ORDER BY roleid);

-- Menu grants (every sc_menu row carrying the code — one code spans several pages).
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT m.menuid, @officer, 1, 1, GETDATE(), GETDATE(), 'migration'
FROM sc_menu m
WHERE m.isactive = 1 AND m.menucode IN ('PAY_RUNS', 'PAY_ADHOC', 'PAY_ADMIN', 'PAY_REPORTS')
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = m.menuid AND x.roleid = @officer);

INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT m.menuid, @approver, 1, CASE WHEN m.menucode = 'PAY_RUNS' THEN 1 ELSE 0 END, GETDATE(), GETDATE(), 'migration'
FROM sc_menu m
WHERE m.isactive = 1 AND m.menucode IN ('PAY_RUNS', 'PAY_REPORTS')
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = m.menuid AND x.roleid = @approver);

-- Per-page rights. Officer: payroll/welfare/employee pages create+read+edit; delete only on
-- runs (= cancel a run before approval). Approver: runs read+edit (approve/post/paid), reports read.
IF @admin IS NOT NULL
BEGIN
    INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
    SELECT @officer, a.progpath, 1, 1, 1, CASE WHEN a.progpath LIKE '/pay/runs%' THEN 1 ELSE 0 END, 1
    FROM sc_program_role a
    WHERE a.roleid = @admin
      AND (a.progpath LIKE '/pay%' OR a.progpath LIKE '/welfare%' OR a.progpath LIKE '/employee%' OR a.progpath LIKE '/admin/super-master%')
      AND NOT EXISTS (SELECT 1 FROM sc_program_role x WHERE x.roleid = @officer AND x.progpath = a.progpath);

    INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
    SELECT @approver, a.progpath, 0, 1, CASE WHEN a.progpath LIKE '/pay/runs%' THEN 1 ELSE 0 END, 0, 1
    FROM sc_program_role a
    WHERE a.roleid = @admin
      AND (a.progpath LIKE '/pay/runs%' OR a.progpath LIKE '/pay/reports%' OR a.progpath LIKE '/pay/dashboard%')
      AND NOT EXISTS (SELECT 1 FROM sc_program_role x WHERE x.roleid = @approver AND x.progpath = a.progpath);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @roles TABLE (roleid bigint);
INSERT INTO @roles SELECT roleid FROM sc_role WHERE rolecode IN ('PAYROLL_OFFICER', 'PAYROLL_APPROVER');
DELETE FROM sc_program_role WHERE roleid IN (SELECT roleid FROM @roles);
DELETE FROM sc_role_menu WHERE roleid IN (SELECT roleid FROM @roles);
UPDATE sc_user_role SET isactive = 0 WHERE roleid IN (SELECT roleid FROM @roles);
UPDATE sc_role SET isactive = 0 WHERE roleid IN (SELECT roleid FROM @roles);
");

            migrationBuilder.DropColumn(
                name: "IsPaidByPayroll",
                table: "Pos_EmployeeType");
        }
    }
}
