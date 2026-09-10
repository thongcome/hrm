-- ============================================================================
--  flow มาตรฐาน — เลิกชี้ชื่อคน หันมาใช้ผังองค์กรกับ role
--
--  CEO, 10 ก.ย. 2569: "1 ขั้นจบน่าจะผิดหมด" และ
--                     "ของเดิม เป็น test ไม่ได้ใช้ แล้ว เคลียร์ ออกได้เลย"
--
--  ปัญหาของเดิม (ตรวจจากข้อมูลจริงก่อนแก้):
--    - 24 แถวใน wf_custom_user ของ workflow งานจริง มี 21 แถวชี้ไปที่
--      admin / advadmin ซึ่งเป็น account ของผู้พัฒนาเอง ไม่ใช่ผู้อนุมัติจริง
--    - wf_custom_role 2 แถวชี้ไปที่ role "admin"
--    - 19 workflow เป็น "1 ขั้นจบ"
--    พอส่งมอบให้ลูกค้า ชื่อพวกนี้ไม่มีอยู่ในองค์กรเขา ทุก workflow จะพังพร้อมกัน
--    และต้องไล่แก้ทีละตัว 20 กว่าที่
--
--  หลักที่ใช้แทน: ผู้อนุมัติต้องมาจาก "ข้อมูลที่องค์กรมีอยู่แล้ว" ไม่ใช่ชื่อที่พิมพ์ไว้
--    - หัวหน้า  -> ผังองค์กร (com_organization.approver_empid) ผ่าน isupperrole
--                 ลูกค้าจัดผังองค์กรของเขาเอง workflow ก็ถูกทันที ไม่ต้องแก้ config
--    - HR      -> role "HR" ผ่าน iscustomRole
--                 ส่งมอบ = ย้าย role ให้คนของเขา ที่เดียว ไม่ใช่แก้ 20 workflow
--
--  รูปแบบมาตรฐาน 2 แบบ (จัดกลุ่มตามว่า "ใครเป็นคนยื่น")
--    A. พนักงานยื่นเรื่องของตัวเอง   ขั้น 1 หัวหน้าตามผังองค์กร -> ขั้น 2 HR (istop)
--    B. HR/หัวหน้าเป็นผู้ยื่นเรื่อง    ขั้น 1 HR -> ขั้น 2 ผู้บริหาร POS_A05 (istop)
--       (ขั้นหัวหน้าไม่มีความหมายเมื่อผู้ยื่นคือ HR เอง เช่น "เปลี่ยนสถานะ workflow")
--
--  ขั้นที่ใช้ role ต้องตั้ง isorcondition = 1 เสมอ
--  เพราะ iscustomRole คืน "ทุกคนที่มี role นั้น" และค่า default ของขั้นคือ
--  ต้องครบทุกคน — ถ้าไม่ตั้ง HR ทุกคนต้องกดอนุมัติทุกใบ
--
--  ไม่แตะ EXPENSE_TRAVEL — เป็น flow ที่ CEO สั่งให้สร้างเอง ตั้งชื่อคนจริงไว้
--  (กาญจนา / ธวัชชัย / ขวัญใจ) และมีงานเดินอยู่ 6 งาน
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ── 1. role "HR" ────────────────────────────────────────────────────────────
--    ระบบมี role ครบทุกอย่างแล้วยกเว้น HR (admin, emp, POS_A03..A07,
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

-- ── 3. workflow ไหนใช้รูปแบบไหน ─────────────────────────────────────────────
IF OBJECT_ID('tempdb..#plan') IS NOT NULL DROP TABLE #plan;
CREATE TABLE #plan (workflowcode nvarchar(100) PRIMARY KEY, pattern char(1));

INSERT INTO #plan (workflowcode, pattern) VALUES
    -- A: พนักงานยื่นเรื่องของตัวเอง -> หัวหน้า -> HR
    ('LEAVE_APPROVAL','A'), ('OT_APPROVAL','A'), ('TIMESHEET_APPROVAL','A'),
    ('ATT_CORRECTION','A'), ('EXPENSE_CLAIM_APPROVAL','A'), ('LMS_TRAINING_APPROVAL','A'),
    ('IDP_APPROVAL','A'), ('REQUISITION_APPROVAL','A'), ('PVD_RATE_CHANGE_APPROVAL','A'),
    ('ENG_REDEEM','A'),
    -- B: HR/หัวหน้าเป็นผู้ยื่นเรื่องเกี่ยวกับพนักงานคนอื่น -> HR -> ผู้บริหาร
    ('DISCIPLINARY_APPROVAL','B'), ('EMPLOYEE_SEPARATION_APPROVAL','B'),
    ('PERF_EVAL_APPROVAL','B'), ('PIP_APPROVAL','B'), ('REWARD_APPROVAL','B'),
    ('SUCCESSION_NOMINATION_APPROVAL','B'), ('HIRE_APPROVAL','B'), ('OFFER_APPROVAL','B'),
    ('ORG_CHANGE_BOSS','B'), ('ORG_CHANGE_MOVE','B'), ('ORG_CHANGE_NEWORG','B'),
    ('KM_ARTICLE_APPROVAL','B'), ('PVD_EXIT_APPROVAL','B'), ('WF_STATE_CHANGE','B');
GO

-- ── 4. ล้าง config ทดสอบของ workflow ที่อยู่ในแผน ───────────────────────────
DELETE c FROM wf_custom_user c
JOIN wf_workflow w ON w.workflowid = c.workflowid
JOIN #plan p ON p.workflowcode = w.workflowcode;

DELETE c FROM wf_custom_role c
JOIN wf_workflow w ON w.workflowid = c.workflowid
JOIN #plan p ON p.workflowcode = w.workflowcode;
GO

-- ── 5. ต้องมี 2 ขั้นเสมอ — ขั้นไหนยังไม่มี สร้างจากขั้น 1 ────────────────────
--    คัดลอกทั้งแถวเพื่อให้ได้ controller/action และค่า default อื่นติดมาด้วย
--    แล้วค่อยเขียนทับเฉพาะช่องที่รูปแบบกำหนด (ขั้นที่ 6)
--    ทุกคอลัมน์ bit ของตารางนี้เป็น NOT NULL และไม่มี default ต้องใส่ให้ครบทุกตัว
INSERT INTO wf_sub_workflow_master (
    subject, workflowid, wlevel, isAdhocUser, iscustomApprover, isupperrole, isupperuser,
    iscustomRole, iscustomUser, iscondition, isorcondition, isandcondition, andpercent,
    forwardstatus, standstatus, backwardstatus, istop, status, isReturnSender,
    isshow, isLOA, isAutoApproveAllow, isNeedBudgetApproval, isPool, sitinstatus,
    isApproverSameOrg, isApproverSameCostCenter, isManualButton,
    controller, action, displayName)
SELECT
    N'ขั้นที่ 2', s.workflowid, 2, 0, 0, 0, 0,
    0, 0, 0, 0, 0, NULL,
    s.forwardstatus, s.standstatus, s.backwardstatus, 0, s.status, 0,
    1, 0, 0, 0, 0, s.sitinstatus,
    0, 0, 0,
    s.controller, s.action, s.displayName
FROM wf_sub_workflow_master s
JOIN wf_workflow w ON w.workflowid = s.workflowid
JOIN #plan p ON p.workflowcode = w.workflowcode
WHERE s.wlevel = 1
  AND NOT EXISTS (SELECT 1 FROM wf_sub_workflow_master x
                  WHERE x.workflowid = s.workflowid AND x.wlevel = 2);
GO

-- ── 6. เขียน config ของทั้งสองขั้นตามรูปแบบ ─────────────────────────────────
--    ล้างทุก flag ที่บอก "ใครอนุมัติ" ก่อน แล้วค่อยติ๊กเฉพาะที่รูปแบบต้องการ
--    (รันซ้ำแล้วได้ผลเดิมเสมอ ไม่มี flag เก่าค้าง)
UPDATE s
SET    s.iscustomUser = 0, s.iscustomRole = 0, s.isupperrole = 0, s.isupperuser = 0,
       s.isAdhocUser = 0, s.isApproverSameOrg = 0, s.isApproverSameCostCenter = 0,
       s.isLOA = 0, s.isorcondition = 0, s.isandcondition = 0, s.andpercent = NULL,
       s.istop = 0
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode;
GO

-- ขั้น 1
UPDATE s
SET    s.subject       = CASE WHEN p.pattern = 'A' THEN N'หัวหน้าตามผังองค์กร' ELSE N'ฝ่ายบุคคล (HR)' END,
       s.isupperrole   = CASE WHEN p.pattern = 'A' THEN 1 ELSE 0 END,
       s.iscustomRole  = CASE WHEN p.pattern = 'A' THEN 0 ELSE 1 END,
       s.isorcondition = CASE WHEN p.pattern = 'A' THEN 0 ELSE 1 END,  -- role = ใครก็ได้ในกลุ่ม
       s.isReturnSender = 1,        -- ขั้นแรกส่งกลับให้ผู้ยื่นแก้ได้
       s.isshow        = 1
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode
WHERE  s.wlevel = 1;

-- ขั้น 2 (istop)
UPDATE s
SET    s.subject       = CASE WHEN p.pattern = 'A' THEN N'ฝ่ายบุคคล (HR)' ELSE N'ผู้บริหาร' END,
       s.iscustomRole  = 1,
       s.isorcondition = 1,
       s.istop         = 1,
       s.isReturnSender = 1,
       s.isshow        = 1
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode
WHERE  s.wlevel = 2;
GO

-- ขั้นที่ 3 ขึ้นไปที่หลงเหลือจาก config เก่า ปิดไม่ให้ใช้ (ไม่ลบ เก็บประวัติไว้)
UPDATE s SET s.isshow = 0, s.istop = 0
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode
WHERE  s.wlevel > 2;
GO

-- ── 7. ผูก role ให้ขั้นที่เป็น role ────────────────────────────────────────
--    ระวัง: wf_custom_role.modby เป็น bigint (userid) ไม่ใช่ชื่อผู้แก้แบบตารางอื่น
INSERT INTO wf_custom_role (workflowid, wlevel, subworkflowid, roleid, rolecode, isactive, modate, modby)
SELECT s.workflowid, s.wlevel, s.subworkflowid, r.roleid, r.rolecode, 1, GETDATE(), NULL
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode
JOIN   sc_role r
       ON r.rolecode = CASE
            WHEN p.pattern = 'A' AND s.wlevel = 2 THEN 'HR'
            WHEN p.pattern = 'B' AND s.wlevel = 1 THEN 'HR'
            WHEN p.pattern = 'B' AND s.wlevel = 2 THEN 'POS_A05'   -- ผู้จัดการฝ่าย
          END
WHERE  s.wlevel IN (1, 2) AND s.iscustomRole = 1
  AND  NOT EXISTS (SELECT 1 FROM wf_custom_role x
                   WHERE x.workflowid = s.workflowid AND x.wlevel = s.wlevel AND x.roleid = r.roleid);
GO

-- ── 8. ผลลัพธ์ ─────────────────────────────────────────────────────────────
PRINT '--- flow หลังตั้งค่า ---';
SELECT w.workflowcode, p.pattern, s.wlevel, s.subject,
       CASE WHEN s.isupperrole = 1 THEN N'หัวหน้าผังองค์กร'
            WHEN s.iscustomRole = 1 THEN N'role: ' + ISNULL((SELECT TOP 1 r.rolecode FROM wf_custom_role c
                   JOIN sc_role r ON r.roleid = c.roleid
                   WHERE c.workflowid = s.workflowid AND c.wlevel = s.wlevel), '(ยังไม่ผูก)')
            ELSE N'** ไม่มีใครอนุมัติ **' END AS approver,
       CAST(s.istop AS int) AS istop,
       (SELECT COUNT(*) FROM sc_user_role ur
        JOIN wf_custom_role c ON c.roleid = ur.roleid
        WHERE c.workflowid = s.workflowid AND c.wlevel = s.wlevel AND ur.isactive = 1) AS peopleInRole
FROM   wf_sub_workflow_master s
JOIN   wf_workflow w ON w.workflowid = s.workflowid
JOIN   #plan p ON p.workflowcode = w.workflowcode
WHERE  s.wlevel IN (1, 2)
ORDER  BY w.workflowcode, s.wlevel;

PRINT '--- ยังมีชื่อคนตายตัวเหลืออยู่ที่ไหนบ้าง (ควรเหลือแต่ EXPENSE_TRAVEL) ---';
SELECT w.workflowcode, c.wlevel, u.loginname
FROM   wf_custom_user c
JOIN   wf_workflow w ON w.workflowid = c.workflowid
JOIN   sc_user u ON u.userid = c.userid
ORDER  BY w.workflowcode, c.wlevel;
GO

DROP TABLE #plan;
GO
