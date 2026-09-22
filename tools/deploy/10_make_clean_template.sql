/* ============================================================================
   10_make_clean_template.sql — turn a COPY of our database into the clean install template
   (handoff #11, ported from Advance.Payroll's own script of the same name — CEO, 19 ก.ย. 2569:
   "deploy ได้โดยให้ข้อมูลตั้งต้นไม่มี demo").

   Run it ONLY on a restored copy whose name contains "template" (e.g. hrm_template) —
   it refuses to touch anything else. It keeps:
     · the system skeleton   menus, programs (AD.CRUDManage per-page rights), roles, workflows +
                             levels + role approvers + buttons, statuses, reasons, doc types,
                             migration history
     · shared defaults       banks, tax brackets / deduction types, SSO rate, positions / levels /
                             exec types, currency, languages
     · company defaults      (company ADVD only — renamed to the customer's code at install by
                             20_new_customer.sql) leave policy / holidays / settings,
                             attendance-deduction/company settings, payslip settings (password
                             parts cleared), GL mapping, employee types
     · one company (ADVD), its root org node, and the system account advadmin (always kept —
                             it is the installer's account; no password survives, see
                             `HRM.dll --init-admin`)
   and empties everything else: demo employees, org units, position slots, users, payroll runs,
   workflow jobs/requests, audit log, and every module's own catalogs/content the customer sets
   up fresh (recruitment postings, LMS courses, competency library, OKRs, engagement campaigns,
   provident-fund/insurance/commission plans, ...) — same "customer configures it after install"
   treatment Advance.Payroll already gives Pay_InsurancePlan/Pay_CommissionPlan.

   HRM has ~340 model classes but only ~165 tables carry any rows in a real database — the wipe
   loop below only ever touches a table that currently has data, so untouched dormant scaffold
   (per CLAUDE.md's "most tables are dormant") is never even opened.
   ========================================================================== */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT OFF;
IF DB_NAME() NOT LIKE '%template%'
BEGIN
    RAISERROR('Refusing: run this only on a copy whose database name contains "template".', 16, 1);
    RETURN;
END

DECLARE @co nvarchar(50) = N'ADVD';
DECLARE @coId bigint = (SELECT id FROM com_company WHERE code = @co);
IF @coId IS NULL BEGIN RAISERROR('company ADVD not found', 16, 1); RETURN; END

-- Step 1 below deletes from ~150 tables (hundreds of thousands of rows on a
-- demo-seeded database — AuditLog alone can be six figures). Deliberately NOT
-- wrapped in one transaction: an install target's transaction log is not
-- guaranteed to be sized for that much uncommitted log in one go (hit exactly
-- this — error 3930, "cannot support operations that write to the log file" —
-- building this script against a modest throwaway copy), and the wipe is
-- naturally idempotent, so autocommitting each DELETE and re-running on
-- failure is both safer and simpler than pre-sizing the log. Step 2 (the
-- company filter) is a much smaller, bounded set of writes and keeps its own
-- transaction below.

-- ── tables kept (then filtered below) ───────────────────────────────────────
CREATE TABLE #keep (tbl sysname PRIMARY KEY);
INSERT #keep VALUES
 ('__EFMigrationsHistory'),
 ('sc_menu'),('sc_menugroup'),('sc_program'),('sc_program_role'),('sc_role'),('sc_role_menu'),('sc_role_program'),
 ('sc_user'),('sc_user_role'),
 ('AspNetUsers'),('AspNetUserRoles'),('AspNetUserClaims'),('AspNetUserLogins'),('AspNetUserTokens'),('AspNetRoles'),('AspNetRoleClaims'),
 ('Sys_ProtectedSetting'),('SystemLanguageSettings'),
 ('mas_address_type'),('mas_doc_type'),('mas_reason'),('mas_title'),('job_status'),('Currency'),('Com_Bank'),('employeetype'),('com_organize_layer'),
 ('com_company'),('com_organization'),
 ('wf_workflow'),('wf_sub_workflow_master'),('wf_subworkflow_field'),('wf_button'),('wf_button_master'),('wf_custom_role'),('WorkflowDesignTable'),
 ('Pay_PayItemType'),('Pay_TaxBracket'),('Pay_TaxDeductionType'),('Pay_TaxDeductionSetting'),('Pay_SeveranceTaxRule'),
 ('Pay_AttendanceDeductionPolicy'),('Pay_PayslipSettings'),('Pay_GLAccountMapping'),('Pay_PaySchedule'),('Pay_BankFileFormat'),
 ('HRUCFSECURITY'),
 ('pos_position'),('pos_position_level'),('Pos_ExecType'),('Pos_EmployeeType'),
 ('Lve_LeaveType'),('Lve_LeavePolicy'),('Lve_CompanyHoliday'),('Lve_CompanySetting'),
 ('Wel_BenefitTypes'),('Att_CompanySetting');
DELETE FROM #keep WHERE tbl NOT IN (SELECT name FROM sys.tables);   -- tolerate a table renamed/removed since this list was written

-- Kept tables still point INTO tables step 1 is about to empty (com_organization's
-- approver/boss point at HREMPLOYEE; sc_user.hremployee_id too) — null those now so
-- HREMPLOYEE (and anything else these reach) isn't permanently blocked by a table
-- that's never touched by the generic wipe below. The real values get set back to
-- NULL for the kept row anyway in step 2; this just does it before step 1 needs it.
UPDATE com_organization SET approver_hremployee_id = NULL, boss_hremployee_id = NULL
WHERE approver_hremployee_id IS NOT NULL OR boss_hremployee_id IS NOT NULL;
UPDATE sc_user SET hremployee_id = NULL WHERE hremployee_id IS NOT NULL;

-- ── 1. empty every other table that currently has rows (generic FK-order retry, same
--       technique Advance.Payroll's script uses — no hand-authored dependency order needed
--       even across HRM's much larger FK graph) ────────────────────────────────────────────
CREATE TABLE #todo (tbl sysname PRIMARY KEY, done bit DEFAULT 0);
INSERT #todo (tbl)
SELECT t.name FROM sys.tables t
WHERE t.name NOT IN (SELECT tbl FROM #keep)
  AND EXISTS (SELECT 1 FROM sys.partitions p WHERE p.object_id = t.object_id AND p.index_id IN (0,1) AND p.rows > 0);

DECLARE @pass int = 0, @left int = 1, @t sysname, @sql nvarchar(max);
WHILE @pass < 25 AND @left > 0
BEGIN
    SET @pass += 1;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT tbl FROM #todo WHERE done = 0;
    OPEN c; FETCH NEXT FROM c INTO @t;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'DELETE FROM ' + QUOTENAME(@t) + N';';
        BEGIN TRY
            EXEC sp_executesql @sql;
            UPDATE #todo SET done = 1 WHERE tbl = @t;
        END TRY
        BEGIN CATCH
            IF @pass = 25 PRINT CONCAT('FAILED ', @t, ': ', ERROR_MESSAGE());
        END CATCH
        FETCH NEXT FROM c INTO @t;
    END
    CLOSE c; DEALLOCATE c;
    SELECT @left = COUNT(*) FROM #todo WHERE done = 0;
    CHECKPOINT;   -- reclaims log space between passes (SIMPLE recovery); harmless no-op under FULL

    -- a still-stuck table pointing at itself (self-referencing FK, e.g. com_organization.parentID
    -- before it's in #keep's filter step, or any future Xxx.ParentId) blocks a plain full-table
    -- DELETE even though the whole table is being emptied — null every self-referencing FK column
    -- on tables not yet done, once per pass, before retrying.
    IF @left > 0
    BEGIN
        DECLARE @selfTbl sysname, @selfCol sysname;
        DECLARE sc CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT t.name, c.name
            FROM sys.foreign_key_columns fkc
            JOIN sys.tables t ON t.object_id = fkc.parent_object_id AND t.object_id = fkc.referenced_object_id
            JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE t.name IN (SELECT tbl FROM #todo WHERE done = 0);
        OPEN sc; FETCH NEXT FROM sc INTO @selfTbl, @selfCol;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'UPDATE ' + QUOTENAME(@selfTbl) + N' SET ' + QUOTENAME(@selfCol) + N' = NULL WHERE ' + QUOTENAME(@selfCol) + N' IS NOT NULL;';
            BEGIN TRY EXEC sp_executesql @sql; END TRY BEGIN CATCH END CATCH
            FETCH NEXT FROM sc INTO @selfTbl, @selfCol;
        END
        CLOSE sc; DEALLOCATE sc;
    END
END

-- Don't proceed to filter/keep-side edits while step 1 left demo data behind —
-- that would produce a template that looks clean but still has orphaned rows.
IF @left > 0
BEGIN
    SELECT tbl AS still_has_rows FROM #todo WHERE done = 0;
    RAISERROR('Step 1 could not empty every table (see list above) — fix and re-run; nothing in step 2 was touched.', 16, 1);
    RETURN;
END

-- ── 2. filter kept tables to company ADVD — any error from here on rolls everything back ──
-- (deliberately XACT_ABORT OFF here too: the retry loop below needs a caught 547 to leave the
-- transaction committable so it can retry in a later pass — XACT_ABORT ON dooms the whole
-- transaction on the very first conflict even though TRY/CATCH catches it, which defeats the
-- retry and always rolls back on the first ordering conflict. The outer CATCH below still
-- rolls back explicitly on any error that isn't resolved by a retry.)
BEGIN TRAN;
BEGIN TRY
-- string company columns: keep only ADVD rows. Kept tables can reference each other
-- (Pos_ExecType -> Pos_EmployeeType) so this needs the same retry-until-clean approach
-- as step 1, just over a much smaller table/row set.
DECLARE @col sysname;
CREATE TABLE #companyFiltered (tbl sysname PRIMARY KEY, col sysname, done bit DEFAULT 0);
INSERT #companyFiltered (tbl, col)
    SELECT t.name, c.name FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE t.name IN (SELECT tbl FROM #keep) AND t.name NOT IN ('com_company','com_organization','sc_user')
      AND c.name IN ('companyid','CompanyId') AND ty.name IN ('nvarchar','varchar','nchar','char');

DECLARE @cfPass int = 0, @cfLeft int = 1;
WHILE @cfPass < 10 AND @cfLeft > 0
BEGIN
    SET @cfPass += 1;
    DECLARE k CURSOR LOCAL FAST_FORWARD FOR SELECT tbl, col FROM #companyFiltered WHERE done = 0;
    OPEN k; FETCH NEXT FROM k INTO @t, @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'DELETE FROM ' + QUOTENAME(@t) + N' WHERE ' + QUOTENAME(@col) + N' IS NULL OR ' + QUOTENAME(@col) + N' <> @co;';
        BEGIN TRY
            EXEC sp_executesql @sql, N'@co nvarchar(50)', @co;
            UPDATE #companyFiltered SET done = 1 WHERE tbl = @t;
        END TRY
        BEGIN CATCH
            IF @cfPass = 10 THROW;   -- out of retries — surface the real FK error, step 2's own CATCH rolls back
        END CATCH
        FETCH NEXT FROM k INTO @t, @col;
    END
    CLOSE k; DEALLOCATE k;
    SELECT @cfLeft = COUNT(*) FROM #companyFiltered WHERE done = 0;
END

-- payslip settings: company defaults stay, the demo's default-password recipe does not
UPDATE Pay_PayslipSettings SET DefaultPasswordPart1 = NULL, DefaultPasswordPart2 = NULL,
       CompanyAddress = NULL, CompanyTaxId = NULL, SsoEmployerAccountNo = NULL;

-- one company + its root org node — clear every pointer to a demo person/unit (id- and
-- code-based: parentID/parent_code from HRMContext.OrgParent.cs, approver_hremployee_id/
-- boss_hremployee_id from HRMContext.EmployeeTypes.cs's sibling hook — see CLAUDE.md "Id and
-- code: two columns, two jobs")
DELETE FROM com_organization WHERE NOT (isCompany = 1 AND comp_code = @co AND companyid = @coId);
UPDATE com_organization SET boss_emp_id = NULL, approver_empid = NULL, approver_userid = NULL,
       boss_hremployee_id = NULL, approver_hremployee_id = NULL,
       boss_name = NULL, approver_name = NULL, approver_PosName = NULL, parent_code = NULL, parentID = NULL;

-- system account only: advadmin (kept always — the installer's account)
DECLARE @admin bigint = (SELECT userid FROM sc_user WHERE loginname = 'advadmin');
IF @admin IS NULL THROW 50001, 'advadmin missing', 1;
DELETE FROM sc_user_role WHERE userid <> @admin;
DELETE FROM AspNetUsers WHERE userid IS NULL OR userid <> @admin;
UPDATE sc_user SET upperuserid = NULL WHERE userid = @admin;
DELETE FROM sc_user WHERE userid <> @admin;
UPDATE sc_user SET empid = NULL, hremployee_id = NULL, isEmployee = 'N', company_id = @coId,
       isforcechanged = 1, lasttimelogin = NULL, invalidpwcount = 0, orgid = NULL, orgcode = NULL, orgname = NULL
WHERE userid = @admin;
-- no usable password survives into the template (set at install: HRM.dll --init-admin)
UPDATE AspNetUsers SET PasswordHash = NULL, SecurityStamp = CONVERT(nvarchar(36), NEWID()), AccessFailedCount = 0, LockoutEnd = NULL
WHERE userid = @admin;
DELETE FROM sc_user_role WHERE userid = @admin AND roleid NOT IN (SELECT roleid FROM sc_role WHERE name = N'Admin');

-- other companies last: sc_role/sc_user carry the old company_id — move the kept roles over,
-- users are already gone (advadmin's row was updated above), then the other companies can go
UPDATE sc_role SET company_id = @coId WHERE company_id <> @coId;
DELETE FROM com_company WHERE id <> @coId;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @msg nvarchar(4000) = CONCAT(N'ROLLED BACK — step 2 failed at line ', ERROR_LINE(), N': ', ERROR_MESSAGE());
    RAISERROR(@msg, 16, 1);
    RETURN;
END CATCH

COMMIT;

-- ── 3. report ───────────────────────────────────────────────────────────────
SELECT (SELECT COUNT(*) FROM HREMPLOYEE) employees, (SELECT COUNT(*) FROM sc_user) users,
       (SELECT COUNT(*) FROM com_company) companies, (SELECT COUNT(*) FROM com_organization) orgs,
       (SELECT COUNT(*) FROM Pay_PayrollRun) runs, (SELECT COUNT(*) FROM job_master) jobs,
       (SELECT COUNT(*) FROM AuditLog) audit_rows, (SELECT COUNT(*) FROM wf_workflow WHERE isactive = 1) active_workflows;
PRINT 'TEMPLATE READY';
