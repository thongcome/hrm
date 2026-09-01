using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class DemoReadinessPolish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Demo-readiness polish (BA/HR review, 1 ก.ย. 2569). All data
            // corrections are SOFT flips/renames — nothing is hard-deleted,
            // audit history stays intact.

            // 1) Labour-law fix: 1,252 ADVD demo employees were "hired"
            //    before age 18 (worst case: age 5) because the seeder drew
            //    age and tenure independently (seeder itself fixed in the
            //    same commit). Shift BIRTH_DATE back so every hire happened
            //    at age 22-36, deterministically varied by row id.
            migrationBuilder.Sql(@"
UPDATE HREMPLOYEE SET BIRTH_DATE = DATEADD(year, -(22 + (ID % 15)), WORK_DATE)
WHERE companyid='ADVD' AND BIRTH_DATE IS NOT NULL AND WORK_DATE IS NOT NULL
  AND WORK_DATE < DATEADD(year, 18, BIRTH_DATE);
");

            // 2) ADVD had ZERO leave policies (ESS leave form showed the
            //    not-configured warning) and no company work-week setting —
            //    clone company 001's configuration.
            migrationBuilder.Sql(@"
INSERT INTO Lve_LeavePolicy (CompanyId, LeaveTypeId, EntitlementDaysPerYear, IsActive, Code, CarryOverMode, IsPaid, MaxCarryOverDays, CarryOverExpiryMonths, MinServiceMonths)
SELECT 'ADVD', p.LeaveTypeId, p.EntitlementDaysPerYear, p.IsActive, p.Code, p.CarryOverMode, p.IsPaid, p.MaxCarryOverDays, p.CarryOverExpiryMonths, p.MinServiceMonths
FROM Lve_LeavePolicy p
WHERE p.CompanyId='001'
  AND NOT EXISTS (SELECT 1 FROM Lve_LeavePolicy x WHERE x.CompanyId='ADVD' AND x.LeaveTypeId=p.LeaveTypeId);

INSERT INTO Lve_CompanySetting (CompanyId, CountryCode, WorkDaysMask)
SELECT 'ADVD', s.CountryCode, s.WorkDaysMask FROM Lve_CompanySetting s
WHERE s.CompanyId='001'
  AND NOT EXISTS (SELECT 1 FROM Lve_CompanySetting x WHERE x.CompanyId='ADVD');
");

            // 3) Thai public holidays 2026 (~15 days) for both companies —
            //    the holiday calendar held ONE day total, which no Thai HR
            //    would believe, and leave-day counting ignored holidays.
            migrationBuilder.Sql(@"
DECLARE @h TABLE (d date, n nvarchar(100));
INSERT INTO @h VALUES
 ('2026-01-01', N'วันขึ้นปีใหม่'),
 ('2026-03-03', N'วันมาฆบูชา'),
 ('2026-04-06', N'วันจักรี'),
 ('2026-04-13', N'วันสงกรานต์'),
 ('2026-04-14', N'วันสงกรานต์'),
 ('2026-04-15', N'วันสงกรานต์'),
 ('2026-05-01', N'วันแรงงานแห่งชาติ'),
 ('2026-05-04', N'วันฉัตรมงคล'),
 ('2026-06-01', N'วันวิสาขบูชา (ชดเชย)'),
 ('2026-06-03', N'วันเฉลิมพระชนมพรรษาสมเด็จพระราชินี'),
 ('2026-07-28', N'วันเฉลิมพระชนมพรรษา ร.10'),
 ('2026-07-29', N'วันอาสาฬหบูชา'),
 ('2026-08-12', N'วันแม่แห่งชาติ'),
 ('2026-10-23', N'วันปิยมหาราช'),
 ('2026-12-07', N'วันพ่อแห่งชาติ (ชดเชย)'),
 ('2026-12-10', N'วันรัฐธรรมนูญ'),
 ('2026-12-31', N'วันสิ้นปี');
INSERT INTO Lve_CompanyHoliday (CompanyId, HolidayDate, Name, IsActive)
SELECT c.companyid, h.d, h.n, 1
FROM (VALUES ('001'), ('ADVD')) c(companyid)
CROSS JOIN @h h
WHERE NOT EXISTS (SELECT 1 FROM Lve_CompanyHoliday x WHERE x.CompanyId=c.companyid AND x.HolidayDate=h.d);
");

            // 4) Test-data hygiene (soft flags only):
            //    - public + logged-in test announcements hidden
            //    - the 3 test org nodes (TESTWF1/2, BR-TEST01) deactivated
            //    - the 3 obviously-test employees (022-024 'ทดสอบ') hidden
            //    - 13 regression/demo workflow definitions hidden from
            //      dropdowns (isshow=0; isactive kept — old jobs reference them)
            //    - the LMS test course renamed to something presentable
            //    - test_payroll's display name now matches its real employee
            //      (สมชาย ใจดี) so ADVD approval chains stop showing a login name
            //    - the adopted /pay/runs menu row renamed (เงินเดือน (ใหม่) -> รอบเงินเดือน)
            migrationBuilder.Sql(@"
UPDATE info_message SET isactive=0 WHERE Id IN (2,3,4,6);
UPDATE com_organization SET isActive=0 WHERE id IN (10014,10015,10016);
UPDATE HREMPLOYEE SET IsActive=0 WHERE companyid='001' AND EMP_NO IN ('022','023','024');
UPDATE wf_workflow SET isshow=0 WHERE workflowid IN (1,2,3,4,5,6,7,21,23,24,10021,10022,10023);
UPDATE Lms_Course SET Title=N'หลักสูตรปฐมนิเทศพนักงานใหม่', Code='ORIENT-001' WHERE Id=1 AND Code='COURSE-TEST';
UPDATE sc_user SET firstname=N'สมชาย', lastname=N'ใจดี' WHERE loginname='test_payroll';
UPDATE sc_menu SET menuname=N'รอบเงินเดือน' WHERE CAST(url AS nvarchar(500)) = N'/pay/runs';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
