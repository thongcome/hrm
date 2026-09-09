-- ============================================================================
--  จัดหมวดหมู่ + ไอคอน ให้ workflow (สำหรับหน้า landing /workflow)
-- ============================================================================
--  wf_workflow มีคอลัมน์ wgroup และ icon อยู่แล้ว ไม่ต้องเพิ่ม schema
--  หน้า landing อ่านสองคอลัมน์นี้ไปจัดกลุ่มและเลือกไอคอนเอง — เพิ่ม workflow
--  ใหม่แล้วอยากให้ขึ้นหมวดไหน ก็ใส่ wgroup ไม่ต้องแก้โค้ดหน้า
--
--  icon = ชื่อไอคอน Material ของ MudBlazor (เช่น EventBusy, Payments)
--  หน้า landing แปลงชื่อเป็นไอคอนจริงด้วย reflection ชื่อที่ไม่รู้จักจะใช้ไอคอนกลาง
--
--  รันซ้ำได้ — ไม่ทับค่าที่ตั้งไว้แล้ว
-- ============================================================================

SET NOCOUNT ON;

UPDATE w SET wgroup = v.grp, icon = v.ico
FROM wf_workflow w
JOIN (VALUES
    -- ── งานบุคคลประจำวัน ──
    ('LEAVE_APPROVAL',                 N'งานบุคคล',        'EventBusy'),
    ('OT_APPROVAL',                    N'งานบุคคล',        'MoreTime'),
    ('ATT_CORRECTION',                 N'งานบุคคล',        'EditCalendar'),
    ('TIMESHEET_APPROVAL',             N'งานบุคคล',        'PunchClock'),
    ('UNIFORM_REQUEST',                N'งานบุคคล',        'Checkroom'),
    ('EMPLOYEE_SEPARATION_APPROVAL',   N'งานบุคคล',        'PersonRemove'),

    -- ── การเงินและสวัสดิการ ──
    ('EXPENSE_TRAVEL',                 N'การเงิน',         'Flight'),
    ('EXPENSE_CLAIM_APPROVAL',         N'การเงิน',         'ReceiptLong'),
    ('WELFARE_CLAIM',                  N'การเงิน',         'VolunteerActivism'),
    ('PVD_RATE_CHANGE_APPROVAL',       N'การเงิน',         'Savings'),
    ('PVD_EXIT_APPROVAL',              N'การเงิน',         'AccountBalanceWallet'),
    ('ENG_REDEEM',                     N'การเงิน',         'Redeem'),

    -- ── สรรหาและอัตรากำลัง ──
    ('REQUISITION_APPROVAL',           N'สรรหา',           'PersonSearch'),
    ('OFFER_APPROVAL',                 N'สรรหา',           'Handshake'),
    ('HIRE_APPROVAL',                  N'สรรหา',           'HowToReg'),

    -- ── พัฒนาบุคลากร ──
    ('IDP_APPROVAL',                   N'พัฒนาบุคลากร',    'TrendingUp'),
    ('LMS_TRAINING_APPROVAL',          N'พัฒนาบุคลากร',    'School'),
    ('PERF_EVAL_APPROVAL',             N'พัฒนาบุคลากร',    'Assessment'),
    ('PIP_APPROVAL',                   N'พัฒนาบุคลากร',    'Psychology'),
    ('SUCCESSION_NOMINATION_APPROVAL', N'พัฒนาบุคลากร',    'Groups3'),
    ('KM_ARTICLE_APPROVAL',            N'พัฒนาบุคลากร',    'MenuBook'),

    -- ── วินัยและรางวัล ──
    ('DISCIPLINARY_APPROVAL',          N'วินัยและรางวัล',   'Gavel'),
    ('REWARD_APPROVAL',                N'วินัยและรางวัล',   'EmojiEvents'),

    -- ── โครงสร้างองค์กร ──
    ('ORG_CHANGE_NEWORG',              N'โครงสร้างองค์กร',  'AccountTree'),
    ('ORG_CHANGE_MOVE',                N'โครงสร้างองค์กร',  'SwapHoriz'),
    ('ORG_CHANGE_BOSS',                N'โครงสร้างองค์กร',  'SupervisorAccount'),
    ('WF_STATE_CHANGE',                N'โครงสร้างองค์กร',  'Settings')
) v(code, grp, ico) ON v.code = w.workflowcode
WHERE w.wgroup IS NULL OR w.wgroup = '';

-- ตัวทดสอบทั้งหมดไปรวมหมวดเดียว จะได้ไม่ปนกับของจริง
UPDATE wf_workflow
   SET wgroup = N'ทดสอบระบบ', icon = ISNULL(icon, 'Science')
 WHERE (wgroup IS NULL OR wgroup = '')
   AND (workflowcode LIKE 'DEMO[_]%' OR workflowcode LIKE 'TEST[_]%' OR workflowcode IN ('T_STEP', 'Bleave'));

SELECT ISNULL(wgroup, N'(ยังไม่จัดหมวด)') AS [หมวด], COUNT(*) AS [จำนวน],
       STRING_AGG(workflowcode, ', ') WITHIN GROUP (ORDER BY workflowcode) AS [workflow]
FROM wf_workflow GROUP BY wgroup ORDER BY COUNT(*) DESC;
