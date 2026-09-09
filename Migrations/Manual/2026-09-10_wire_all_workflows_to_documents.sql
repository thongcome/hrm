-- ============================================================================
--  ผูกทุก workflow เข้ากับเอกสารต้นทาง เพื่อย้ายมาใช้หน้า /workflow ตัวใหม่
-- ============================================================================
--  CEO, 10 ก.ย. 2569: "ทยอย config workflow ได้เลย เปลี่ยน workflow ทั้งหมดมาเป็นอันนี้"
--
--  งานที่ไม่มีเอกสารให้ดู = ผู้อนุมัติไม่รู้ว่ากำลังอนุมัติอะไร สคริปต์นี้จึงเติมสองอย่าง
--  ให้ครบทุก workflow ที่รู้ต้นทาง:
--
--    wf_workflow.tableref              งานนี้อนุมัติเอกสารจากตารางไหน
--                                      (อ่านมาจาก StartJobAsync ของแต่ละโมดูล)
--    wf_sub_workflow_master.controller  route หน้าเอกสารที่จะเสียบให้ผู้อนุมัติดู
--                                      คัดลอกจาก wf_workflow.url ที่ตั้งไว้แล้ว
--
--  เดิม url ถูกใช้เป็น "ลิงก์กระโดดออกไปดู" เท่านั้น พอใส่ลงเป็น controller ของ
--  แต่ละระดับ หน้าอนุมัติจะเสียบเอกสารเข้ามาในหน้าเดียวกันเลย
--
--  ทำเฉพาะ workflow ที่ url มี {refid} — ตัวที่ url ชี้ไปหน้ารายการรวม (ไม่มี
--  {refid}) ยังเสียบไม่ได้เพราะไม่รู้ว่าจะเปิดเอกสารไหน ปล่อยไว้เป็นลิงก์เหมือนเดิม
--
--  รันซ้ำได้ — ไม่ทับค่าที่ตั้งไว้แล้ว
-- ============================================================================

SET NOCOUNT ON;

-- ── 1. tableref: งานนี้อนุมัติเอกสารตารางไหน ──────────────────────────────
UPDATE w SET tableref = v.tbl
FROM wf_workflow w
JOIN (VALUES
    ('ATT_CORRECTION',                 'Att_CorrectionRequest'),
    ('TIMESHEET_APPROVAL',             'Att_TimesheetSubmission'),
    ('UNIFORM_REQUEST',                'Emp_UniformRequest'),
    ('EXPENSE_CLAIM_APPROVAL',         'Exp_ClaimHeader'),
    ('EXPENSE_TRAVEL',                 'Exp_ClaimHeader'),
    ('DISCIPLINARY_APPROVAL',          'Hr_DisciplinaryCase'),
    ('REWARD_APPROVAL',                'Hr_RewardCase'),
    ('EMPLOYEE_SEPARATION_APPROVAL',   'Hr_SeparationRequest'),
    ('IDP_APPROVAL',                   'Idp_Plan'),
    ('KM_ARTICLE_APPROVAL',            'Km_Article'),
    ('LEAVE_APPROVAL',                 'Lve_LeaveRequest'),
    ('LMS_TRAINING_APPROVAL',          'Lms_Enrollment'),
    ('ORG_CHANGE_NEWORG',              'Org_OrganizationChangeRequest'),
    ('ORG_CHANGE_MOVE',                'Org_OrganizationChangeRequest'),
    ('ORG_CHANGE_BOSS',                'Org_OrganizationChangeRequest'),
    ('PVD_EXIT_APPROVAL',              'Pay_ProvidentFundExitCase'),
    ('PVD_RATE_CHANGE_APPROVAL',       'Pay_ProvidentFundRateChangeRequest'),
    ('PERF_EVAL_APPROVAL',             'Perf_EvaluationInstance'),
    ('PIP_APPROVAL',                   'Perf_ImprovementPlan'),
    ('HIRE_APPROVAL',                  'Rec_Offer'),
    ('OFFER_APPROVAL',                 'Rec_Offer'),
    ('REQUISITION_APPROVAL',           'Rec_Requisition'),
    ('SUCCESSION_NOMINATION_APPROVAL', 'Succ_SuccessorNomination'),
    ('WELFARE_CLAIM',                  'Wel_Claim'),
    ('OT_APPROVAL',                    'HrwOt')
) v(code, tbl) ON v.code = w.workflowcode
WHERE w.tableref IS NULL OR w.tableref = '';

-- ── 2. controller: หน้าเอกสารที่จะเสียบให้ผู้อนุมัติดูในหน้าอนุมัติเลย ────────
--     คัดลอกจาก url ของ workflow เฉพาะตัวที่มี {refid} และระดับที่ยังไม่ได้ตั้ง
UPDATE s SET controller = CAST(w.url AS nvarchar(250))
FROM wf_sub_workflow_master s
JOIN wf_workflow w ON w.workflowid = s.workflowid
WHERE (s.controller IS NULL OR s.controller = '')
  AND w.url IS NOT NULL
  AND CAST(w.url AS nvarchar(400)) LIKE '%{refid}%';

-- ── รายงานผล ────────────────────────────────────────────────────────────
SELECT w.workflowcode,
       ISNULL(w.tableref, '—')                          AS [ตารางเอกสาร],
       ISNULL(CAST(w.url AS nvarchar(120)), '—')        AS [route เอกสาร],
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid)                            AS [ขั้น],
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid AND s.controller IS NOT NULL) AS [ขั้นที่เสียบเอกสารได้],
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid AND s.istop = 1)             AS [ขั้นสุดท้าย]
FROM wf_workflow w
ORDER BY w.workflowcode;
