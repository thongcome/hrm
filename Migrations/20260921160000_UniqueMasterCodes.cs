using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // code = ตัวตนที่ธุรกิจรู้ และเป็น key ที่ใช้จับคู่ตอน import / ย้ายข้อมูล (id ของสองระบบไม่มีวันตรงกัน) — จึงต้องไม่ซ้ำในขอบเขตของมัน
    // (CEO, 21 ก.ย. 2569: "ผมใส่ทั้ง id (คอมพิวเตอร์รู้) และ code (ธุรกิจรู้) ไว้เผื่อทำ data migration") — แบบเดียวกับ Advance.Payroll 20260921140000
    // HRM เป็นหลายบริษัท: master ที่มีคอลัมน์บริษัท ขอบเขตคือ "ต่อบริษัท" เสมอ (CompanyId string ตามเดิม ไม่แตะ convention)
    //   ยกเว้นที่ขอบเขตจริงเป็นอย่างอื่น: Comp_Competency ต่อหมวด · Pay_TaxDeductionType ต่อปีภาษี · Perf_Topic ต่อประเภทการประเมิน
    //   ไม่มีคอลัมน์บริษัท = ทั้งระบบ (com_company, Com_Bank, Pay_PayItemType, sc_user.loginname, wf_workflow, pos_position, Skill, Perf_Indicator/SubTopic)
    // ข้อมูลซ้ำที่เจอใน hrm มีคู่เดียว: หน่วยงาน Ploan (id 9 ปิดใช้แล้ว / id 10) → ตัวที่ปิดใช้ได้รหัสต่อท้าย ~id (ยังไม่มี production: แก้ข้อมูล ไม่ลดกติกา)
    // ยังไม่ทำ: sc_menu.menucode (ซ้ำ 31 กลุ่มจากระบบ JSP เดิม — ต้องตัดสินใจทางธุรกิจก่อน จดเป็นหนี้ใน code-index-baseline.txt)
    // บนฐานลูกค้า: index ตัวไหนลงไม่ได้เพราะรหัสซ้ำ จะข้ามและพิมพ์ชื่อ ไม่แก้ข้อมูลลูกค้าเอง
    public partial class UniqueMasterCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- หน่วยงานรหัสซ้ำในบริษัทเดียวกัน: เก็บตัวที่ใช้งานอยู่ ที่เหลือซึ่ง 'ปิดใช้แล้ว' และไม่มีลูก/พนักงาน ได้รหัสต่อท้าย ~id
;WITH d AS (
    SELECT id, code, isActive,
           ROW_NUMBER() OVER (PARTITION BY companyid, code ORDER BY CASE WHEN isActive = 1 THEN 0 ELSE 1 END, id) AS rn
    FROM com_organization WHERE ISNULL(code, '') <> '')
UPDATE o SET code = LEFT(o.code, 40) + '~' + CAST(o.id AS nvarchar(20))
FROM com_organization o JOIN d ON d.id = o.id
WHERE d.rn > 1 AND o.isActive = 0
  AND NOT EXISTS (SELECT 1 FROM com_organization c WHERE c.parentID = o.id)
  AND NOT EXISTS (SELECT 1 FROM HREMPLOYEE e WHERE e.OrganizationId = o.id);

-- (ตาราง, คอลัมน์ขอบเขต หรือ NULL = ทั้งระบบ, คอลัมน์ code) — ชุดเดียวกับ SchemaConventionTests.MasterCodes
DECLARE @ix TABLE (tbl sysname, scope sysname NULL, code sysname);
INSERT @ix VALUES
 ('com_company', NULL, 'code'), ('com_organization', 'companyid', 'code'), ('HREMPLOYEE', 'companyid', 'EMP_NO'),
 ('sc_role', 'company_id', 'rolecode'), ('sc_user', NULL, 'loginname'), ('wf_workflow', NULL, 'workflowcode'), ('pos_position', NULL, 'code'),
 ('Com_Bank', NULL, 'Code'), ('Com_ChartOfAccount', 'CompanyId', 'Code'), ('Com_SectionType', 'CompanyId', 'Code'), ('Com_SubSectionType', 'CompanyId', 'Code'),
 ('Pos_PositionSlot', 'CompanyId', 'PosCode'), ('Pos_ExecType', 'CompanyId', 'Code'), ('Pos_EmployeeType', 'CompanyId', 'Code'),
 ('Pos_HeadcountBudget', 'CompanyId', 'BudgetCode'), ('Job_Family', 'CompanyId', 'Code'), ('Job_Level', 'CompanyId', 'Code'),
 ('Att_ShiftDefinition', 'CompanyId', 'ShiftCode'), ('Att_OtRule', 'CompanyId', 'Code'), ('Att_Project', 'CompanyId', 'Code'),
 ('Att_Device', 'CompanyId', 'DeviceCode'), ('Att_GeofenceLocation', 'CompanyId', 'Code'),
 ('Lve_LeavePolicy', 'CompanyId', 'Code'), ('Lve_CompanyHoliday', 'CompanyId', 'Code'),
 ('Pay_PayItemType', NULL, 'Code'), ('Pay_PaySchedule', 'CompanyId', 'Code'),
 ('Pay_SalaryGrade', 'CompanyId', 'GradeCode'), ('Pay_InsurancePlan', 'CompanyId', 'PlanCode'), ('Pay_ProvidentFundPolicy', 'CompanyId', 'PolicyCode'),
 ('Pay_ProvidentFundInvestmentPolicy', 'CompanyId', 'Code'), ('Pay_WelfareFundPolicy', 'CompanyId', 'Code'), ('Pay_TaxDeductionType', 'EffectiveYear', 'Code'),
 ('Exp_ExpenseCategory', 'CompanyId', 'Code'), ('Hrd_LifecycleTaskTemplate', 'CompanyId', 'Code'),
 ('Comp_Category', 'CompanyId', 'Code'), ('Comp_Competency', 'CategoryId', 'Code'), ('Skill', NULL, 'Code'), ('Skill_Category', 'CompanyId', 'Code'),
 ('Perf_EvaluationType', 'CompanyId', 'Code'), ('Perf_EvaluationPeriod', 'CompanyId', 'Code'), ('Perf_RaterDirectionConfig', 'CompanyId', 'Code'),
 ('Perf_Topic', 'EvaluationTypeId', 'Code'), ('Perf_Indicator', NULL, 'Code'), ('Perf_SubTopic', NULL, 'Code'),
 ('Okr_Cycle', 'CompanyId', 'Code'), ('Okr_GoalCategory', 'CompanyId', 'Code'),
 ('Lms_Course', 'CompanyId', 'Code'), ('Lms_CourseCategory', 'CompanyId', 'Code'), ('Km_ArticleCategory', 'CompanyId', 'Code'),
 ('Eng_QuestionTemplate', 'CompanyId', 'Code'), ('Eng_RedeemItem', 'CompanyId', 'Code'), ('Eng_SurveyCampaign', 'CompanyId', 'Code'),
 ('Succ_KeyPosition', 'CompanyId', 'Code'), ('Rec_Requisition', 'CompanyId', 'RequisitionCode');

DECLARE @tbl sysname, @scope sysname, @code sysname, @name sysname, @sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT tbl, scope, code FROM @ix;
OPEN c; FETCH NEXT FROM c INTO @tbl, @scope, @code;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @name = CONCAT('UXC_', @tbl, '_', ISNULL(@scope + '_', ''), @code);
    IF OBJECT_ID(@tbl) IS NOT NULL AND COL_LENGTH(@tbl, @code) IS NOT NULL AND (@scope IS NULL OR COL_LENGTH(@tbl, @scope) IS NOT NULL)
       -- มี unique index ที่มีคอลัมน์ code อยู่ในคีย์แล้ว = ข้าม (เช่น Wel_BenefitTypes, Pay_CommissionPlan, Lve_LeaveType)
       AND NOT EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                       WHERE i.object_id = OBJECT_ID(@tbl) AND i.is_unique = 1 AND COL_NAME(ic.object_id, ic.column_id) = @code)
    BEGIN
        SET @sql = N'CREATE UNIQUE INDEX ' + QUOTENAME(@name) + N' ON ' + QUOTENAME(@tbl) + N' (' + ISNULL(QUOTENAME(@scope) + N', ', N'') + QUOTENAME(@code)
                 + N') WHERE ' + QUOTENAME(@code) + N' IS NOT NULL';
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT CONCAT('UNIQUE INDEX ADDED ', @name);
        END TRY
        BEGIN CATCH PRINT CONCAT('UNIQUE INDEX SKIPPED ', @name, ': ', ERROR_MESSAGE()); END CATCH
    END
    FETCH NEXT FROM c INTO @tbl, @scope, @code;
END
CLOSE c; DEALLOCATE c;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // index ของ migration นี้ขึ้นต้น UXC_ (unique code) ทั้งหมด — ของเดิมในฐานใช้ IX_ / UQ_ / UX_ จึงไม่โดนลบไปด้วย
            migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
FROM sys.indexes i WHERE i.name LIKE 'UXC[_]%' AND i.is_unique = 1 AND i.is_primary_key = 0;
EXEC sp_executesql @sql;");
        }
    }
}
