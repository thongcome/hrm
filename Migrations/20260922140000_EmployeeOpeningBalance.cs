using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ยอดยกมา — ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: "ดู ใน Advance.Payroll เลย
    // ระบบต้องเหมือน Mirror ใน งานเงินเดือน" — payroll-domain gap found by a full route/behavior
    // diff against Advance.Payroll, not just its commit log). One row per employee per month a
    // company already paid from its OLD system before going live mid-year on HumanOk — distinct
    // from Pay_EmployeePriorEmployerIncome (a genuinely different employer). See
    // Model/Pay_EmployeeOpeningBalance.cs for what reads it.
    public partial class EmployeeOpeningBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE Pay_EmployeeOpeningBalance (
    Id bigint NOT NULL IDENTITY(1,1) CONSTRAINT PK_Pay_EmployeeOpeningBalance PRIMARY KEY,
    CompanyId nvarchar(50) NOT NULL,
    HremployeeId bigint NOT NULL,
    EmpNo nvarchar(50) NOT NULL,
    TaxYear int NOT NULL,
    Month int NOT NULL,
    GrossIncome decimal(15,2) NOT NULL,
    TaxableIncome decimal(15,2) NOT NULL,
    TaxWithheld decimal(15,2) NOT NULL,
    SsoEmployee decimal(15,2) NOT NULL,
    SsoEmployer decimal(15,2) NOT NULL,
    PvdEmployee decimal(15,2) NOT NULL,
    PvdEmployer decimal(15,2) NOT NULL,
    NetPay decimal(15,2) NULL,
    ImportBatchId bigint NULL,
    IsActive bit NOT NULL,
    UpdatedByUserId bigint NOT NULL,
    UpdatedAt datetime2 NOT NULL,
    CONSTRAINT CK_Pay_EmployeeOpeningBalance_Month CHECK ([Month] BETWEEN 1 AND 12)
);

CREATE UNIQUE INDEX UX_Pay_EmployeeOpeningBalance_Emp_Year_Month ON Pay_EmployeeOpeningBalance (HremployeeId, TaxYear, Month);
CREATE INDEX IX_Pay_EmployeeOpeningBalance_Company_Year ON Pay_EmployeeOpeningBalance (CompanyId, TaxYear);

ALTER TABLE Pay_EmployeeOpeningBalance WITH CHECK ADD CONSTRAINT FK_Pay_EmployeeOpeningBalance_HREMPLOYEE
    FOREIGN KEY (HremployeeId) REFERENCES HREMPLOYEE (ID);
ALTER TABLE Pay_EmployeeOpeningBalance WITH CHECK ADD CONSTRAINT FK_Pay_EmployeeOpeningBalance_Hr_EmployeeImportBatch
    FOREIGN KEY (ImportBatchId) REFERENCES Hr_EmployeeImportBatch (Id);
CREATE INDEX IX_Pay_EmployeeOpeningBalance_ImportBatchId ON Pay_EmployeeOpeningBalance (ImportBatchId);

ALTER TABLE Hr_EmployeeImportBatch ADD OpeningBalancesImported int NOT NULL CONSTRAINT DF_Hr_EmployeeImportBatch_OpeningBalancesImported DEFAULT 0;
");

            // menu ""ยอดยกมา"" next to ""เงินได้จากนายจ้างเดิม"" (same group/parent), granted to the same roles —
            // ScMenuNavCatalog also carries this route so ScMenuNavSeeder adopts (not recreates) this row on next startup
            migrationBuilder.Sql(@"
DECLARE @src bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/prior-employer-income' AND isactive = 1 ORDER BY menuid);
IF @src IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/opening-balances')
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, langcode, isshow, icon, url, uppermenucode, menugroupid, isactive, menuname_en, moddate, modby)
    SELECT N'ยอดยกมาจากระบบเดิม', menulevel, isfinal, ISNULL(menuorder, 0) + 1, menucode, langcode, 1, 'Icons.Material.Filled.History',
           '/pay/admin/opening-balances', uppermenucode, menugroupid, 1, N'Opening balances (previous system)', GETDATE(), 'ScMenuNavSeeder'
    FROM sc_menu WHERE menuid = @src;

DECLARE @menu bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/opening-balances');
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT @menu, x.roleid, 1, x.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_role_menu x WHERE x.menuid = @src AND x.isactive = 1 AND @menu IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu y WHERE y.menuid = @menu AND y.roleid = x.roleid);

-- page rights: Admin all; HR / payroll officer (who import it) may switch a month off; everyone else is seeded read-only at startup
INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
SELECT r.roleid, '/pay/admin/opening-balances', CASE WHEN r.name = N'Admin' THEN 1 ELSE 0 END, 1, 1, 1, 1
FROM sc_role r WHERE (r.name = N'Admin' OR r.rolecode IN ('HR', 'PAYROLL_OFFICER')) AND r.isactive = 1
  AND NOT EXISTS (SELECT 1 FROM sc_program_role p WHERE p.roleid = r.roleid AND p.progpath = '/pay/admin/opening-balances');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE sc_menu SET isactive = 0 WHERE CAST(url AS nvarchar(400)) = '/pay/admin/opening-balances';
ALTER TABLE Hr_EmployeeImportBatch DROP CONSTRAINT DF_Hr_EmployeeImportBatch_OpeningBalancesImported;
ALTER TABLE Hr_EmployeeImportBatch DROP COLUMN OpeningBalancesImported;
DROP TABLE Pay_EmployeeOpeningBalance;
");
        }
    }
}
