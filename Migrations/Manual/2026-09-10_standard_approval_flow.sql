-- ============================================================================
--  flow มาตรฐาน — เลิกชี้ชื่อคน หันมาใช้ผังองค์กรกับ role
--
--  CEO, 10 ก.ย. 2569: "1 ขั้นจบน่าจะผิดหมด"
--                     "ของเดิม เป็น test ไม่ได้ใช้ แล้ว เคลียร์ ออกได้เลย"
--   และรอบแก้: "ปรับ%กองทุน -> น่าจะไป HR เลย, แลกของรางวัล -> น่าจะ auto redeem,
--               B. น่าจะหลากหลาย"
--
--  ปัญหาของเดิม (ตรวจจากข้อมูลจริงก่อนแก้):
--    - 24 แถวใน wf_custom_user ของ workflow งานจริง มี 21 แถวชี้ไปที่
--      admin / advadmin ซึ่งเป็น account ของผู้พัฒนาเอง ไม่ใช่ผู้อนุมัติจริง
--    - wf_custom_role 2 แถวชี้ไปที่ role "admin"
--    - 19 workflow เป็น "1 ขั้นจบ"
--    พอส่งมอบให้ลูกค้า ชื่อพวกนี้ไม่มีอยู่ในองค์กรเขา ทุก workflow พังพร้อมกัน
--
--  หลักที่ใช้แทน: ผู้อนุมัติต้องมาจาก "ข้อมูลที่องค์กรมีอยู่แล้ว" ไม่ใช่ชื่อที่พิมพ์ไว้
--    SUP  = หัวหน้าตามผังองค์กร (com_organization.approver_empid) ผ่าน isupperrole
--           ลูกค้าจัดผังองค์กรของเขาเอง workflow ถูกทันที ไม่ต้องแก้ config
--    ROLE = ทุกคนที่ถือ role นั้น ผ่าน iscustomRole
--           ส่งมอบ = ย้าย role ที่เดียว ไม่ใช่แก้ทีละ workflow
--
--  ระดับความอาวุโสของขั้นสุดท้าย เลือกตามน้ำหนักของเรื่อง ไม่ใช่แบบเดียวใช้ทุกที่:
--    HR      เรื่องปกติของพนักงาน
--    POS_A05 ผู้จัดการฝ่าย   — เรื่องที่มีผลกับเงิน/สิทธิรายบุคคล
--    POS_A06 ผู้อำนวยการฝ่าย — เรื่องที่กระทบสถานะการจ้างหรือวินัย
--    POS_A07 CEO            — เรื่องที่กระทบโครงสร้างองค์กรและค่าตอบแทนแรกเข้า
--  และขั้นแรกคือ "คนที่ต้องรับรองข้อเท็จจริง" — ถ้าเรื่องเป็นของพนักงานคนหนึ่ง
--  หัวหน้าเขาเป็นคนรับรองก่อนเสมอ ถ้าเป็นเรื่องที่ HR เป็นผู้ตั้งเรื่องเอง
--  ขั้นหัวหน้าไม่มีความหมาย จึงเริ่มที่ HR
--
--  ขั้นที่ใช้ ROLE ต้องตั้ง isorcondition = 1 เสมอ
--  เพราะ iscustomRole คืน "ทุกคนที่มี role นั้น" และค่า default ของขั้นคือ
--  ต้องครบทุกคน — ถ้าไม่ตั้ง HR ทุกคนต้องกดอนุมัติทุกใบ
--
--  ไม่แตะ EXPENSE_TRAVEL — flow ที่ CEO สั่งให้สร้างเอง ตั้งชื่อคนจริงไว้
--  (กาญจนา / ธวัชชัย / ขวัญใจ) และมีงานเดินอยู่ 6 งาน
--
--  กับดัก schema ที่เจอตอนทำ (เขียนไว้กันคนถัดไปเสียเวลา):
--    - คอลัมน์ bit ทุกตัวของ wf_sub_workflow_master เป็น NOT NULL และไม่มี
--      default ต้องระบุให้ครบทุกตัวตอน INSERT
--    - wf_custom_role.modby เป็น bigint (userid) ไม่ใช่ชื่อผู้แก้แบบตารางอื่น
--
--  รันซ้ำได้ — ล้าง flag ก่อนเขียนใหม่ทุกครั้ง ผลลัพธ์จึงเหมือนเดิมเสมอ
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ── 1. role "HR" ────────────────────────────────────────────────────────────
--    sc_role มีครบทุกอย่างแล้วยกเว้น HR (admin, emp, POS_A03..A07,
--    ORG_APPROVER, GUEST, VENDOR, COMMITTEE) เพิ่มแบบ add-missing-by-rolecode
--    ตามแบบเดียวกับ PositionRoleSeeder ไม่แตะแถวที่มีอยู่
IF NOT EXISTS (SELECT 1 FROM sc_role WHERE rolecode = 'HR')
    INSERT INTO sc_role (company_id, name, abbr, rolelevel, rolecode, isactive, isHeader, moddate, modby)
    SELECT TOP 1 company_id, N'ฝ่ายบุคคล (HR)', 'HR', '1', 'HR', 1, 0, GETDATE(), 'standard_approval_flow.sql'
    FROM sc_role ORDER BY roleid;
GO

-- ── 2. ให้ role HR กับคนที่ถือ role admin อยู่ตอนนี้ ────────────────────────
--    ขั้น HR ต้องมีคนจริง ไม่งั้นงานเดินไปแล้วหาผู้รับไม่เจอ
--    ตอนส่งมอบ ลูกค้าถอด role นี้จาก admin แล้วให้คนของเขาแทน — ที่เดียวจบ
INSERT INTO sc_user_role (userid, empid, roleid, isactive, modate, modby)
SELECT ur.userid, ur.empid, hr.roleid, 1, GETDATE(), 'standard_approval_flow.sql'
FROM   sc_user_role ur
JOIN   sc_role adm ON adm.roleid = ur.roleid AND adm.rolecode = 'admin'
CROSS  JOIN (SELECT roleid FROM sc_role WHERE rolecode = 'HR') hr
WHERE  ur.isactive = 1
  AND  NOT EXISTS (SELECT 1 FROM sc_user_role x
                   WHERE x.userid = ur.userid AND x.roleid = hr.roleid);
GO

-- ── 3. เส้นทางของแต่ละ workflow — แก้ที่ตารางนี้ที่เดียว ────────────────────
--    kind: SUP = หัวหน้าตามผังองค์กร, ROLE = ทุกคนที่ถือ rolecode ที่ระบุ
IF OBJECT_ID('tempdb..#flow') IS NOT NULL DROP TABLE #flow;
CREATE TABLE #flow (
    workflowcode nvarchar(100),
    wlevel       int,
    kind         varchar(4),
    rolecode     nvarchar(50) NULL,
    subject      nvarchar(200),
    PRIMARY KEY (workflowcode, wlevel));

INSERT INTO #flow (workflowcode, wlevel, kind, rolecode, subject) VALUES
-- ── พนักงานยื่นเรื่องของตัวเอง: หัวหน้ารับรอง แล้ว HR ปิด ────────────────────
 ('LEAVE_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'), ('LEAVE_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('OT_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),    ('OT_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('TIMESHEET_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'), ('TIMESHEET_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('ATT_CORRECTION',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),  ('ATT_CORRECTION',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('LMS_TRAINING_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'), ('LMS_TRAINING_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('IDP_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),    ('IDP_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
-- เบิกค่าใช้จ่ายมีตัวเงิน จบที่ผู้จัดการฝ่าย
 ('EXPENSE_CLAIM_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('EXPENSE_CLAIM_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('EXPENSE_CLAIM_APPROVAL',3,'ROLE','POS_A05',N'ผู้จัดการฝ่าย'),
-- ขอซื้อ/เบิกของ มีตัวเงินเช่นกัน
 ('REQUISITION_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('REQUISITION_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('REQUISITION_APPROVAL',3,'ROLE','POS_A05',N'ผู้จัดการฝ่าย'),

-- ── หัวหน้าตั้งเรื่องเกี่ยวกับลูกน้อง: หัวหน้ารับรองข้อเท็จจริงก่อน ──────────
 ('PERF_EVAL_APPROVAL',1,'SUP',NULL,N'หัวหน้าผู้ประเมิน'), ('PERF_EVAL_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('KM_ARTICLE_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'), ('KM_ARTICLE_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('PIP_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('PIP_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('PIP_APPROVAL',3,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('REWARD_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('REWARD_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('REWARD_APPROVAL',3,'ROLE','POS_A05',N'ผู้จัดการฝ่าย'),
-- วินัยและการพ้นสภาพ กระทบสถานะการจ้าง จบที่ผู้อำนวยการฝ่าย
 ('DISCIPLINARY_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('DISCIPLINARY_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('DISCIPLINARY_APPROVAL',3,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('EMPLOYEE_SEPARATION_APPROVAL',1,'SUP',NULL,N'หัวหน้าตามผังองค์กร'),
 ('EMPLOYEE_SEPARATION_APPROVAL',2,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('EMPLOYEE_SEPARATION_APPROVAL',3,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),

-- ── HR เป็นผู้ตั้งเรื่องเอง: ไม่มีขั้นหัวหน้า เริ่มที่ HR ────────────────────
--    CEO: "ปรับ%กองทุน -> น่าจะไป HR เลย"
 ('PVD_RATE_CHANGE_APPROVAL',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('PVD_RATE_CHANGE_APPROVAL',2,'ROLE','POS_A05',N'ผู้จัดการฝ่าย'),
 ('PVD_EXIT_APPROVAL',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('PVD_EXIT_APPROVAL',2,'ROLE','POS_A05',N'ผู้จัดการฝ่าย'),
 ('HIRE_APPROVAL',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('HIRE_APPROVAL',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('WF_STATE_CHANGE',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('WF_STATE_CHANGE',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
-- ค่าตอบแทนแรกเข้าและโครงสร้างองค์กร จบที่ CEO
 ('OFFER_APPROVAL',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('OFFER_APPROVAL',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('OFFER_APPROVAL',3,'ROLE','POS_A07',N'ประธานเจ้าหน้าที่บริหาร'),
 ('SUCCESSION_NOMINATION_APPROVAL',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('SUCCESSION_NOMINATION_APPROVAL',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('SUCCESSION_NOMINATION_APPROVAL',3,'ROLE','POS_A07',N'ประธานเจ้าหน้าที่บริหาร'),
 ('ORG_CHANGE_BOSS',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('ORG_CHANGE_BOSS',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('ORG_CHANGE_BOSS',3,'ROLE','POS_A07',N'ประธานเจ้าหน้าที่บริหาร'),
 ('ORG_CHANGE_MOVE',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('ORG_CHANGE_MOVE',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('ORG_CHANGE_MOVE',3,'ROLE','POS_A07',N'ประธานเจ้าหน้าที่บริหาร'),
 ('ORG_CHANGE_NEWORG',1,'ROLE','HR',N'ฝ่ายบุคคล (HR)'),
 ('ORG_CHANGE_NEWORG',2,'ROLE','POS_A06',N'ผู้อำนวยการฝ่าย'),
 ('ORG_CHANGE_NEWORG',3,'ROLE','POS_A07',N'ประธานเจ้าหน้าที่บริหาร'),

-- ── แลกของรางวัล: ไม่ต้องมีใครอนุมัติ ────────────────────────────────────────
--    CEO: "แลกของรางวัล -> น่าจะ auto redeem"
--    ตั้ง wf_workflow.isautoapprove ข้างล่าง งานจึงปิดตัวเองตอนยื่น
--    ขั้นนี้คงไว้เป็นปลายทางบนกระดาษเท่านั้น (engine ต้องการอย่างน้อยหนึ่งขั้น)
 ('ENG_REDEEM',1,'ROLE','HR',N'ฝ่ายบุคคล (HR) — ปกติไม่ถูกใช้ เพราะอนุมัติอัตโนมัติ');
GO

-- ── 4. ล้าง config ทดสอบของ workflow ที่อยู่ในแผน ───────────────────────────
DELETE c FROM wf_custom_user c
JOIN wf_workflow w ON w.workflowid = c.workflowid
WHERE EXISTS (SELECT 1 FROM #flow f WHERE f.workflowcode = w.workflowcode);

DELETE c FROM wf_custom_role c
JOIN wf_workflow w ON w.workflowid = c.workflowid
WHERE EXISTS (SELECT 1 FROM #flow f WHERE f.workflowcode = w.workflowcode);
GO

-- ── 5. สร้างขั้นที่ยังไม่มีให้ครบตามแผน ─────────────────────────────────────
--    คัดลอกทั้งแถวจากขั้น 1 เพื่อให้ได้ controller/action ติดมาด้วย
--    ทุกคอลัมน์ bit เป็น NOT NULL ไม่มี default จึงต้องระบุครบทุกตัว
INSERT INTO wf_sub_workflow_master (
    subject, workflowid, wlevel, isAdhocUser, iscustomApprover, isupperrole, isupperuser,
    iscustomRole, iscustomUser, iscondition, isorcondition, isandcondition, andpercent,
    forwardstatus, standstatus, backwardstatus, istop, status, isReturnSender,
    isshow, isLOA, isAutoApproveAllow, isNeedBudgetApproval, isPool, sitinstatus,
    isApproverSameOrg, isApproverSameCostCenter, isManualButton,
    controller, action, displayName)
SELECT
    f.subject, s.workflowid, f.wlevel, 0, 0, 0, 0,
    0, 0, 0, 0, 0, NULL,
    s.forwardstatus, s.standstatus, s.backwardstatus, 0, s.status, 0,
    1, 0, 0, 0, 0, s.sitinstatus,
    0, 0, 0,
    s.controller, s.action, s.displayName
FROM   #flow f
JOIN   wf_workflow w ON w.workflowcode = f.workflowcode
JOIN   wf_sub_workflow_master s ON s.workflowid = w.workflowid AND s.wlevel = 1
WHERE  NOT EXISTS (SELECT 1 FROM wf_sub_workflow_master x
                   WHERE x.workflowid = w.workflowid AND x.wlevel = f.wlevel);
GO

-- ── 6. ล้าง flag "ใครอนุมัติ" ทั้งหมดก่อน แล้วเขียนใหม่ตามแผน ───────────────
UPDATE s
SET    s.iscustomUser = 0, s.iscustomRole = 0, s.isupperrole = 0, s.isupperuser = 0,
       s.isAdhocUser = 0, s.isApproverSameOrg = 0, s.isApproverSameCostCenter = 0,
       s.isLOA = 0, s.isorcondition = 0, s.isandcondition = 0, s.andpercent = NULL,
       s.istop = 0, s.isshow = 0
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
WHERE  EXISTS (SELECT 1 FROM #flow f WHERE f.workflowcode = w.workflowcode);
GO

UPDATE s
SET    s.subject        = f.subject,
       s.isupperrole    = CASE WHEN f.kind = 'SUP'  THEN 1 ELSE 0 END,
       s.iscustomRole   = CASE WHEN f.kind = 'ROLE' THEN 1 ELSE 0 END,
       s.isorcondition  = CASE WHEN f.kind = 'ROLE' THEN 1 ELSE 0 END,  -- ใครก็ได้ในกลุ่ม
       s.isReturnSender = 1,        -- ทุกขั้นส่งกลับให้ผู้ยื่นแก้ได้
       s.isshow         = 1,
       s.istop          = CASE WHEN f.wlevel = (SELECT MAX(x.wlevel) FROM #flow x
                                                WHERE x.workflowcode = f.workflowcode)
                               THEN 1 ELSE 0 END
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #flow f ON f.workflowcode = w.workflowcode AND f.wlevel = s.wlevel;
GO

-- ── 7. ผูก role ให้ขั้นที่เป็น ROLE ────────────────────────────────────────
--    ระวัง: wf_custom_role.modby เป็น bigint (userid) ไม่ใช่ชื่อผู้แก้
INSERT INTO wf_custom_role (workflowid, wlevel, subworkflowid, roleid, rolecode, isactive, modate, modby)
SELECT s.workflowid, s.wlevel, s.subworkflowid, r.roleid, r.rolecode, 1, GETDATE(), NULL
FROM   #flow f
JOIN   wf_workflow w ON w.workflowcode = f.workflowcode
JOIN   wf_sub_workflow_master s ON s.workflowid = w.workflowid AND s.wlevel = f.wlevel
JOIN   sc_role r ON r.rolecode = f.rolecode
WHERE  f.kind = 'ROLE'
  AND  NOT EXISTS (SELECT 1 FROM wf_custom_role x
                   WHERE x.workflowid = s.workflowid AND x.wlevel = s.wlevel AND x.roleid = r.roleid);
GO

-- ── 8. แลกของรางวัลอนุมัติอัตโนมัติ ────────────────────────────────────────
UPDATE wf_workflow SET isautoapprove = 1 WHERE workflowcode = 'ENG_REDEEM';
UPDATE wf_workflow SET isautoapprove = 0
WHERE  isautoapprove = 1 AND workflowcode <> 'ENG_REDEEM'
  AND  EXISTS (SELECT 1 FROM #flow f WHERE f.workflowcode = wf_workflow.workflowcode);
GO

-- ── 9. ผลลัพธ์ ─────────────────────────────────────────────────────────────
PRINT '--- flow หลังตั้งค่า ---';
SELECT w.workflowcode,
       CASE WHEN w.isautoapprove = 1 THEN N'อนุมัติอัตโนมัติ' ELSE '' END AS auto,
       s.wlevel, s.subject,
       CASE WHEN s.isupperrole = 1 THEN N'หัวหน้าผังองค์กร'
            WHEN s.iscustomRole = 1 THEN N'role ' + ISNULL((SELECT TOP 1 r.rolecode FROM wf_custom_role c
                   JOIN sc_role r ON r.roleid = c.roleid
                   WHERE c.workflowid = s.workflowid AND c.wlevel = s.wlevel), N'(ยังไม่ผูก)')
            ELSE N'** ไม่มีใครอนุมัติ **' END AS approver,
       CAST(s.istop AS int) AS istop,
       (SELECT COUNT(*) FROM sc_user_role ur
        JOIN wf_custom_role c ON c.roleid = ur.roleid
        WHERE c.workflowid = s.workflowid AND c.wlevel = s.wlevel AND ur.isactive = 1) AS people
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #flow f ON f.workflowcode = w.workflowcode AND f.wlevel = s.wlevel
ORDER  BY w.workflowcode, s.wlevel;

PRINT '--- ตรวจความถูกต้อง: ทุก workflow ต้องมี istop เดียว และทุกขั้นต้องมีผู้อนุมัติ ---';
SELECT w.workflowcode,
       SUM(CASE WHEN s.istop = 1 AND s.isshow = 1 THEN 1 ELSE 0 END) AS tops,
       SUM(CASE WHEN s.isshow = 1 AND s.isupperrole = 0 AND s.iscustomRole = 0
                     AND s.iscustomUser = 0 THEN 1 ELSE 0 END)       AS levelsWithNobody
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
WHERE  EXISTS (SELECT 1 FROM #flow f WHERE f.workflowcode = w.workflowcode)
GROUP  BY w.workflowcode
HAVING SUM(CASE WHEN s.istop = 1 AND s.isshow = 1 THEN 1 ELSE 0 END) <> 1
    OR SUM(CASE WHEN s.isshow = 1 AND s.isupperrole = 0 AND s.iscustomRole = 0
                    AND s.iscustomUser = 0 THEN 1 ELSE 0 END) > 0;

PRINT '--- ยังมีชื่อคนตายตัวเหลืออยู่ที่ไหนบ้าง (ควรเหลือแต่ EXPENSE_TRAVEL กับ workflow ทดสอบ) ---';
SELECT DISTINCT w.workflowcode
FROM   wf_custom_user c JOIN wf_workflow w ON w.workflowid = c.workflowid
ORDER  BY w.workflowcode;
GO

DROP TABLE #flow;
GO
