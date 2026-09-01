using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class Step3RoleMappingAndRenameDemoCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employeetype_code",
                table: "sc_role",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // CEO order (1 ก.ย. 2569): rename the demo company AUTOX/AutoX →
            // ADVD/AdvanceDigital across every table the DemoCompanySeeder
            // (formerly AutoxDemoSeeder) touched. Prefix rewrites are scoped
            // (LIKE 'AX%' inside AUTOX-marked rows only) — real employees
            // ('001'.., admin, test_payroll) never match. The seeder itself
            // now produces ADVD from scratch, so its one-shot guard keeps
            // holding after this rename.
            migrationBuilder.Sql(@"
UPDATE com_company SET code='ADVD', abbr='ADVD',
    name=N'บริษัท แอดวานซ์ ดิจิทัล จำกัด',
    name_en='AdvanceDigital Co., Ltd.',
    remark=N'บริษัทตัวอย่างสำหรับเดโม (สร้างโดย DemoCompanySeeder — ข้อมูลสมมติทั้งหมด)'
WHERE code='AUTOX';

UPDATE com_organization SET comp_code='ADVD',
    code='AD'+SUBSTRING(code,3,98),
    parent_code=CASE WHEN parent_code LIKE 'AX%' THEN 'AD'+SUBSTRING(parent_code,3,498) ELSE parent_code END,
    approver_empid=CASE WHEN approver_empid LIKE 'AX%' THEN 'AD'+SUBSTRING(approver_empid,3,98) ELSE approver_empid END,
    name=REPLACE(name, N'ออโต้เอ็กซ์', N'แอดวานซ์ ดิจิทัล')
WHERE comp_code='AUTOX';

UPDATE HREMPLOYEE SET companyid='ADVD',
    EMP_NO='AD'+SUBSTRING(EMP_NO,3,10),
    orgcode=CASE WHEN orgcode LIKE 'AX%' THEN 'AD'+SUBSTRING(orgcode,3,98) ELSE orgcode END
WHERE companyid='AUTOX';

UPDATE sc_user SET loginname='AD'+SUBSTRING(loginname,3,498), empid='AD'+SUBSTRING(empid,3,98) WHERE empid LIKE 'AX%';
UPDATE sc_user_role SET empid='AD'+SUBSTRING(empid,3,98) WHERE empid LIKE 'AX%';
UPDATE Pos_ExecType SET CompanyId='ADVD' WHERE CompanyId='AUTOX';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "employeetype_code",
                table: "sc_role");
        }
    }
}
