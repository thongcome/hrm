using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // Employee import from Excel (CEO, 18–19 ก.ย. 2569): history table with the file checksum (warns on
    // re-uploading the same file), the menu item under the payroll-admin menu (same group/parent as
    // "ข้อมูลพนักงาน / จัดการพนักงาน"), and per-page rights so HR / payroll officers can import.
    // Menu grants follow whoever already has /pay/employees. Idempotent, ids looked up by url/code.
    public partial class EmployeeImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hr_EmployeeImportBatch",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    FileSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrgsAdded = table.Column<int>(type: "int", nullable: false),
                    OrgsUpdated = table.Column<int>(type: "int", nullable: false),
                    EmployeesAdded = table.Column<int>(type: "int", nullable: false),
                    EmployeesUpdated = table.Column<int>(type: "int", nullable: false),
                    LoginsCreated = table.Column<int>(type: "int", nullable: false),
                    Warnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImportedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Hr_EmployeeImportBatch", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Hr_EmployeeImportBatch_CompanyId_FileSha256",
                table: "Hr_EmployeeImportBatch",
                columns: new[] { "CompanyId", "FileSha256" });

            migrationBuilder.Sql(@"
DECLARE @src bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/employees' AND isactive = 1 ORDER BY menuid);
IF @src IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/hr/employee-import')
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, langcode, isshow, icon, url, uppermenucode, menugroupid, isactive, menuname_en, moddate, modby)
    SELECT N'นำเข้าพนักงานจาก Excel', menulevel, isfinal, ISNULL(menuorder, 0) + 1, menucode, langcode, 1, 'Icons.Material.Filled.UploadFile',
           '/hr/employee-import', uppermenucode, menugroupid, 1, N'Import employees (Excel)', GETDATE(), 'migration'
    FROM sc_menu WHERE menuid = @src;

DECLARE @menu bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/hr/employee-import');
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT @menu, x.roleid, 1, 1, GETDATE(), GETDATE(), 'migration'
FROM sc_role_menu x WHERE x.menuid = @src AND x.isactive = 1 AND @menu IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu y WHERE y.menuid = @menu AND y.roleid = x.roleid);

-- page rights: Admin all; HR and payroll officer create/read/edit; everyone else is seeded read-only at startup
INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
SELECT r.roleid, '/hr/employee-import', 1, 1, 1, CASE WHEN r.name = N'Admin' THEN 1 ELSE 0 END, 1
FROM sc_role r WHERE (r.name = N'Admin' OR r.rolecode IN ('HR', 'PAYROLL_OFFICER')) AND r.isactive = 1
  AND NOT EXISTS (SELECT 1 FROM sc_program_role p WHERE p.roleid = r.roleid AND p.progpath = '/hr/employee-import');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE sc_menu SET isactive = 0 WHERE CAST(url AS nvarchar(400)) = '/hr/employee-import';");
            migrationBuilder.DropTable(name: "Hr_EmployeeImportBatch");
        }
    }
}
