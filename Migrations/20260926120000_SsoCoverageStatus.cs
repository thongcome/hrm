using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ผู้ประกันตน ม.33: HRUCFSECURITY.MaxEntryAge (NULL = 60 ตามกฎหมาย) — เริ่มงานหลังวันครบอายุนี้ไม่หักประกันสังคม;
    // Pay_EmployeeSsoCoverage = สถานะที่ HR กำหนดรายคน (มีผลตั้งแต่-ถึง) ทับค่าที่ระบบคิด; เมนู /pay/admin/sso-coverage
    // ข้าง "เงินได้จากนายจ้างเดิม" ให้บทบาทเดียวกัน
    public partial class SsoCoverageStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('HRUCFSECURITY', 'MaxEntryAge') IS NULL
    ALTER TABLE HRUCFSECURITY ADD MaxEntryAge int NULL;

CREATE TABLE Pay_EmployeeSsoCoverage (
    Id bigint NOT NULL IDENTITY(1,1) CONSTRAINT PK_Pay_EmployeeSsoCoverage PRIMARY KEY,
    HremployeeId bigint NOT NULL,
    IsInsured bit NOT NULL,
    EffectiveFrom date NOT NULL,
    EffectiveTo date NULL,
    Reason nvarchar(200) NULL,
    IsActive bit NOT NULL,
    Note nvarchar(500) NULL,
    EnteredByUserId bigint NOT NULL,
    EnteredDate datetime2 NOT NULL
);
CREATE INDEX IX_Pay_EmployeeSsoCoverage_HremployeeId ON Pay_EmployeeSsoCoverage (HremployeeId);
CREATE INDEX IX_Pay_EmployeeSsoCoverage_EnteredByUserId ON Pay_EmployeeSsoCoverage (EnteredByUserId);
ALTER TABLE Pay_EmployeeSsoCoverage WITH CHECK ADD CONSTRAINT FK_Pay_EmployeeSsoCoverage_HREMPLOYEE
    FOREIGN KEY (HremployeeId) REFERENCES HREMPLOYEE (ID);
ALTER TABLE Pay_EmployeeSsoCoverage WITH CHECK ADD CONSTRAINT FK_Pay_EmployeeSsoCoverage_sc_user
    FOREIGN KEY (EnteredByUserId) REFERENCES sc_user (userid);
");

            migrationBuilder.Sql(@"
DECLARE @src bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/prior-employer-income' AND isactive = 1 ORDER BY menuid);
IF @src IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/sso-coverage')
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, langcode, isshow, icon, url, uppermenucode, menugroupid, isactive, menuname_en, moddate, modby)
    SELECT N'สถานะผู้ประกันตน (ประกันสังคม)', menulevel, isfinal, ISNULL(menuorder, 0) + 2, menucode, langcode, 1, 'Icons.Material.Filled.HealthAndSafety',
           '/pay/admin/sso-coverage', uppermenucode, menugroupid, 1, N'SSO insured status', GETDATE(), 'ScMenuNavSeeder'
    FROM sc_menu WHERE menuid = @src;

DECLARE @menu bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/sso-coverage');
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT @menu, x.roleid, 1, x.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_role_menu x WHERE x.menuid = @src AND x.isactive = 1 AND @menu IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu y WHERE y.menuid = @menu AND y.roleid = x.roleid);

-- page rights: Admin all; HR / payroll officer may set and close a status; everyone else is seeded read-only at startup
INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
SELECT r.roleid, '/pay/admin/sso-coverage', 1, 1, 1, 1, 1
FROM sc_role r WHERE (r.name = N'Admin' OR r.rolecode IN ('HR', 'PAYROLL_OFFICER')) AND r.isactive = 1
  AND NOT EXISTS (SELECT 1 FROM sc_program_role p WHERE p.roleid = r.roleid AND p.progpath = '/pay/admin/sso-coverage');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE sc_menu SET isactive = 0 WHERE CAST(url AS nvarchar(400)) = '/pay/admin/sso-coverage';
DROP TABLE Pay_EmployeeSsoCoverage;
IF COL_LENGTH('HRUCFSECURITY', 'MaxEntryAge') IS NOT NULL ALTER TABLE HRUCFSECURITY DROP COLUMN MaxEntryAge;
");
        }
    }
}
