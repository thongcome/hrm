using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // กติกา id / code (skill advance-data-discipline Part A0, CEO 21 ก.ย. 2569): การอ้างอิงข้ามตารางต้องเป็น id ที่มี FK จริง
    // แบบเดียวกับ Advance.Payroll 20260921130000_MissingForeignKeys (28 ตัว) — HRM มีโมดูลครบจึงมากกว่า สำรวจ hrm วันนี้เจอ ~180 คอลัมน์
    // migration นี้ปิดกลุ่มที่ปลายทางชัดเจน:
    //   1. ตามชื่อคอลัมน์ (ทุกตารางของผลิตภัณฑ์): …HremployeeId → HREMPLOYEE · …OrganizationId → com_organization · …PosExecTypeId → Pos_ExecType
    //      · …PositionSlotId → Pos_PositionSlot · …PayrollRunId → Pay_PayrollRun · …DocCenterId → doc_center · AdhocPayItemId → Pay_AdhocPayItem
    //   2. รายการระบุตรง: แม่-ลูกภายในโมดูล (Lms/Rec/Km/Eng/Perf/…) ที่ model เขียนไว้ว่า "soft-link → X"
    // ที่ยังไม่ทำ (จดเป็นหนี้ใน HRM.Tests/Schema/fk-baseline.txt): …ByUserId / …UserId → sc_user, JobMasterId → job_master, และ log/อ้างอิงหลายปลายทาง
    //
    // เพิ่มแบบ NO ACTION (ไม่ cascade: ระบบนี้ soft delete) + index และ "เฉพาะคอลัมน์ที่ข้อมูลสะอาด" — มีแถวกำพร้า/ชนิดไม่ตรง = ข้ามและพิมพ์ชื่อ
    // ไม่ลบข้อมูลใครเพื่อให้ FK ลงได้ · รันซ้ำได้ (ข้ามตัวที่มีแล้ว)
    public partial class MissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @fk TABLE (tbl sysname, col sysname, reftbl sysname, refcol sysname);

-- 1) ตามชื่อคอลัมน์ ในตารางของผลิตภัณฑ์ (ขอบเขตเดียวกับ SchemaConventionTests.ProductTables)
INSERT @fk
SELECT t.name, c.name, r.reftbl, r.refcol
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id AND ty.name IN ('bigint', 'int')
JOIN (VALUES ('%HremployeeId', 'HREMPLOYEE', 'ID'), ('%OrganizationId', 'com_organization', 'id'), ('%PosExecTypeId', 'Pos_ExecType', 'Id'),
             ('%PositionSlotId', 'Pos_PositionSlot', 'Id'), ('%PayrollRunId', 'Pay_PayrollRun', 'Id'), ('%DocCenterId', 'doc_center', 'id'),
             ('AdhocPayItemId', 'Pay_AdhocPayItem', 'Id')) r(pattern, reftbl, refcol) ON c.name LIKE r.pattern
WHERE c.is_identity = 0
  AND (t.name COLLATE Latin1_General_BIN LIKE '[A-Z][a-z]%[_]%' OR t.name IN ('HREMPLOYEE', 'com_organization'))
  AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                  WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id);

-- 2) ระบุตรง
INSERT @fk VALUES
 ('com_organization','companyid','com_company','id'), ('com_organization','SubSectionTypeId','Com_SubSectionType','Id'),
 ('Org_OrganizationChangeRequest','NewSubSectionTypeId','Com_SubSectionType','Id'), ('sc_program_role','roleid','sc_role','roleid'),
 ('HREMPLOYEE','SalaryGradeId','Pay_SalaryGrade','Id'), ('Hrd_BankAccount','BankId','Com_Bank','Id'),
 ('Hrd_LifecycleTaskInstance','TemplateId','Hrd_LifecycleTaskTemplate','Id'),
 ('Career_PathStep','JobFamilyId','Job_Family','Id'), ('Pos_ExecType','JobFamilyId','Job_Family','Id'), ('Pos_ExecType','JobLevelId','Job_Level','Id'),
 ('Pos_ExecType','EmployeeTypeId','Pos_EmployeeType','Id'), ('Pos_PositionSlot','EmployeeTypeId','Pos_EmployeeType','Id'),
 ('Employee_Skill','SkillId','Skill','Id'), ('Job_SkillRequirement','SkillId','Skill','Id'), ('Skill_Endorsement','EmployeeSkillId','Employee_Skill','Id'),
 ('Eng_ActionPlan','CampaignId','Eng_SurveyCampaign','Id'), ('Eng_CampaignQuestion','SourceTemplateId','Eng_QuestionTemplate','Id'),
 ('Eng_RedeemRequest','RedeemItemId','Eng_RedeemItem','Id'), ('Eng_SurveyCampaign','RelaunchedFromCampaignId','Eng_SurveyCampaign','Id'),
 ('Info_MessageReadLog','InfoMessageId','info_message','Id'), ('Info_MessageTarget','InfoMessageId','info_message','Id'),
 ('Km_Article','CategoryId','Km_ArticleCategory','Id'), ('Km_Article','ProjectId','Att_Project','Id'), ('Km_ArticleComment','ArticleId','Km_Article','Id'),
 ('Km_ExpertEndorsement','ExpertProfileId','Km_ExpertProfile','Id'), ('Km_ExpertProfile','CompetencyId','Comp_Competency','Id'),
 ('Lms_Course','CategoryId','Lms_CourseCategory','Id'), ('Lms_Course','CompetencyId','Comp_Competency','Id'),
 ('Lms_CourseRequirement','CourseId','Lms_Course','Id'), ('Lms_CourseSession','CourseId','Lms_Course','Id'), ('Lms_QuizQuestion','CourseId','Lms_Course','Id'),
 ('Lms_MandatoryAssignment','CourseId','Lms_Course','Id'), ('Lms_MandatoryAssignment','RequirementId','Lms_CourseRequirement','Id'),
 ('Lms_Enrollment','CourseSessionId','Lms_CourseSession','Id'), ('Lms_QuizAttempt','EnrollmentId','Lms_Enrollment','Id'),
 ('Lms_QuizAnswer','QuestionId','Lms_QuizQuestion','Id'), ('Lms_QuizAnswer','QuizAttemptId','Lms_QuizAttempt','Id'),
 ('OrgDev_ChangeMilestones','InitiativeId','OrgDev_ChangeInitiative','Id'), ('OrgDev_LeadershipMilestones','PlanId','OrgDev_LeadershipPlans','Id'),
 ('Pay_ProvidentFundRateChangeRequest','WindowId','Pay_ProvidentFundRateChangeWindow','Id'),
 ('Pay_PayScheduleChangeLog','PayScheduleId','Pay_PaySchedule','Id'), ('Pay_PayScheduleChangeLog','OverrideId','Pay_EmployeePayScheduleOverride','Id'),
 ('Perf_CalibrationAdjustment','InstanceId','Perf_EvaluationInstance','Id'), ('Perf_ImprovementPlan','PreviousPlanId','Perf_ImprovementPlan','Id'),
 ('Perf_ImprovementPlan','SourceEvaluationInstanceId','Perf_EvaluationInstance','Id'), ('Perf_RatingScaleDescription','EvaluationTypeId','Perf_EvaluationType','Id'),
 ('Talent_PotentialRating','EvaluationPeriodId','Perf_EvaluationPeriod','Id'),
 ('Rec_Application','CandidateId','Rec_Candidate','Id'), ('Rec_Application','JobPostingId','Rec_JobPosting','Id'),
 ('Rec_CandidateEducation','CandidateId','Rec_Candidate','Id'), ('Rec_CandidateExperience','CandidateId','Rec_Candidate','Id'),
 ('Rec_Interview','ApplicationId','Rec_Application','Id'), ('Rec_Offer','ApplicationId','Rec_Application','Id'),
 ('Rec_InterviewPanelist','InterviewId','Rec_Interview','Id'), ('Rec_InterviewScorecard','InterviewId','Rec_Interview','Id'),
 ('Rec_JobPosting','RequisitionId','Rec_Requisition','Id'), ('Rec_ScorecardCompetencyRatings','CompetencyId','Comp_Competency','Id'),
 ('Rec_ScorecardCompetencyRatings','ScorecardId','Rec_InterviewScorecard','Id'),
 ('Succ_SuccessorNominations','KeyPositionId','Succ_KeyPosition','Id'),
 ('Wf_ApproverDelegation','WorkflowId','wf_workflow','workflowid'), ('Wf_WorkflowStateChangeRequest','TargetWorkflowId','wf_workflow','workflowid');

DECLARE @tbl sysname, @col sysname, @reftbl sysname, @refcol sysname, @name sysname, @sql nvarchar(max), @orphans int;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT tbl, col, reftbl, refcol FROM @fk ORDER BY tbl, col;
OPEN c; FETCH NEXT FROM c INTO @tbl, @col, @reftbl, @refcol;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @name = CONCAT('FK_', @tbl, '_', @col, '__', @reftbl);
    IF OBJECT_ID(@tbl) IS NOT NULL AND COL_LENGTH(@tbl, @col) IS NOT NULL AND OBJECT_ID(@reftbl) IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_key_columns f
                       WHERE f.parent_object_id = OBJECT_ID(@tbl) AND COL_NAME(f.parent_object_id, f.parent_column_id) = @col)
    BEGIN
        SET @sql = N'SELECT @n = COUNT(1) FROM ' + QUOTENAME(@tbl) + N' c WHERE c.' + QUOTENAME(@col) + N' IS NOT NULL AND NOT EXISTS (SELECT 1 FROM '
                 + QUOTENAME(@reftbl) + N' p WHERE p.' + QUOTENAME(@refcol) + N' = c.' + QUOTENAME(@col) + N')';
        EXEC sp_executesql @sql, N'@n int OUTPUT', @n = @orphans OUTPUT;
        IF @orphans = 0
        BEGIN
            SET @sql = N'ALTER TABLE ' + QUOTENAME(@tbl) + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@name) + N' FOREIGN KEY (' + QUOTENAME(@col)
                     + N') REFERENCES ' + QUOTENAME(@reftbl) + N' (' + QUOTENAME(@refcol) + N')';
            BEGIN TRY
                EXEC sp_executesql @sql;
                PRINT CONCAT('FK ADDED ', @tbl, '.', @col, ' -> ', @reftbl);
                IF NOT EXISTS (SELECT 1 FROM sys.index_columns ic WHERE ic.object_id = OBJECT_ID(@tbl) AND ic.key_ordinal = 1 AND COL_NAME(ic.object_id, ic.column_id) = @col)
                BEGIN
                    SET @sql = N'CREATE INDEX ' + QUOTENAME(CONCAT('IX_', @tbl, '_', @col)) + N' ON ' + QUOTENAME(@tbl) + N' (' + QUOTENAME(@col) + N')';
                    EXEC sp_executesql @sql;
                END
            END TRY
            BEGIN CATCH
                PRINT CONCAT('FK SKIPPED ', @tbl, '.', @col, ': ', ERROR_MESSAGE());
            END CATCH
        END
        ELSE PRINT CONCAT('FK SKIPPED ', @tbl, '.', @col, ' -> ', @reftbl, ': ', @orphans, ' orphan row(s) - clean the data, then re-run this block');
    END
    FETCH NEXT FROM c INTO @tbl, @col, @reftbl, @refcol;
END
CLOSE c; DEALLOCATE c;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ชื่อ FK ของ migration นี้มี __ (ขีดล่างคู่) คั่นตารางปลายทาง — ไม่มี FK อื่นในฐานที่ตั้งชื่อแบบนี้
            migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.foreign_keys WHERE name LIKE 'FK[_]%[_][_]%';
EXEC sp_executesql @sql;");
        }
    }
}
