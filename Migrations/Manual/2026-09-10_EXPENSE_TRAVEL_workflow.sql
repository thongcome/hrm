-- ============================================================================
--  EXPENSE_TRAVEL — ใบเบิกค่าเดินทางของพนักงาน
-- ============================================================================
--  เส้นทาง: ผู้ขอ -> หัวหน้า 3 ระดับ -> HR -> การเงิน -> บัญชี (จบ)
--
--  subworkflow มี 4 ระดับ (wlevel 1-4) — ระดับ 0 ไม่ต้องสร้าง
--  CEO, 10 ก.ย. 2569: "0 ไม่ต้องสร้างสิ มันอัตโนมัติ เป็น draft (บันทึกข้อมูลใน table ref)"
--  ระดับ 0 คือสถานะของงานตอนอยู่ในมือผู้กรอก ก่อนกดส่ง ไม่ใช่ขั้นที่ตั้งค่า
--
--    wlevel 1  หัวหน้า      isNeedsupervisorapprove = 3
--                            หัวหน้าชั้น 1 เซ็น -> งานไม่เดิน jobseq +1 ประทับรอยเท้า
--                            -> ส่งต่อชั้น 2 -> ชั้น 3 -> ครบแล้วค่อยไป wlevel 2
--    wlevel 2  HR           iscustomUser -> ผู้อนุมัติของ AD-HRM
--    wlevel 3  การเงิน       iscustomUser -> ผู้อนุมัติของ AD-FIN-02
--    wlevel 4  บัญชี         iscustomUser -> ผู้อนุมัติของ AD-FIN-01 · istop = จบงาน
--
--  ขั้น 2-4 ติ๊ก isReturnSender = หลังบ้านส่งกลับถึงผู้กรอกได้เลย ไม่ต้องไล่ถอย
--  ผ่านหัวหน้าทั้ง 3 คน และแจ้งทุกคนที่เคยเกี่ยวข้อง
--  ขั้น 1 ไม่ติ๊ก = หัวหน้าส่งกลับได้ทีละขั้น ไปหาคนที่ส่งงานมาให้
--
--  ผู้อนุมัติขั้น 2-4 อ่านจาก com_organization.approver_empid ณ เวลาที่รันสคริปต์
--  ไม่ hardcode ชื่อคน — เปลี่ยนตัวได้ที่ /wf/sub-workflow-levels หรือรันสคริปต์ซ้ำ
--
--  รันซ้ำได้ — ถ้ามีงานผูกอยู่แล้วจะไม่แตะของเดิม
-- ============================================================================

SET NOCOUNT ON;

DECLARE @hr      bigint = (SELECT MAX(u.userid) FROM com_organization o JOIN sc_user u ON u.empid = o.approver_empid WHERE o.code = 'AD-HRM');
DECLARE @finance bigint = (SELECT MAX(u.userid) FROM com_organization o JOIN sc_user u ON u.empid = o.approver_empid WHERE o.code = 'AD-FIN-02');
DECLARE @account bigint = (SELECT MAX(u.userid) FROM com_organization o JOIN sc_user u ON u.empid = o.approver_empid WHERE o.code = 'AD-FIN-01');

IF @hr IS NULL OR @finance IS NULL OR @account IS NULL
BEGIN
    RAISERROR('ยังไม่ได้ตั้ง approver_empid ให้ AD-HRM / AD-FIN-02 / AD-FIN-01 ครบ', 16, 1);
    RETURN;
END

DECLARE @old bigint = (SELECT MAX(workflowid) FROM wf_workflow WHERE workflowcode = 'EXPENSE_TRAVEL');
IF @old IS NOT NULL AND NOT EXISTS (SELECT 1 FROM job_master WHERE workflowid = @old)
BEGIN
    DELETE FROM wf_custom_user         WHERE workflowid = @old;
    DELETE FROM wf_sub_workflow_master WHERE workflowid = @old;
    DELETE FROM wf_workflow            WHERE workflowid = @old;
    SET @old = NULL;
END

IF @old IS NOT NULL
BEGIN
    PRINT 'EXPENSE_TRAVEL มีงานผูกอยู่แล้ว — ไม่แตะของเดิม';
    RETURN;
END

DECLARE @id bigint = (SELECT ISNULL(MAX(workflowid), 0) + 1 FROM wf_workflow);
SET IDENTITY_INSERT wf_workflow ON;
INSERT INTO wf_workflow (workflowid, wname, wstatus, isshow, isactive, workflowcode)
VALUES (@id, N'ใบเบิกค่าเดินทาง', N'ACTIVE', 1, 1, 'EXPENSE_TRAVEL');
SET IDENTITY_INSERT wf_workflow OFF;

INSERT INTO wf_sub_workflow_master
 (workflowid, wlevel, subject, isAdhocUser, iscustomApprover, isupperrole, isupperuser, iscustomRole, iscustomUser,
  iscondition, isorcondition, isandcondition, forwardstatus, standstatus, backwardstatus, istop, isReturnSender,
  isshow, isLOA, isAutoApproveAllow, isNeedBudgetApproval, isPool, isApproverSameOrg, isApproverSameCostCenter,
  isManualButton, sitinstatus, isNeedsupervisorapprove, displayName)
VALUES
 (@id, 1, N'หัวหน้าตามสายบังคับบัญชา (3 ระดับ)', 0,0,0,0,0,0, 0,0,0,'PENDING','PENDING','RETURNED',  0,0, 1,0,1,0,0,0,0,0, NULL, 3,    N'อนุมัติ'),
 (@id, 2, N'ฝ่ายทรัพยากรบุคคล (HR)',            0,0,0,0,0,1, 0,0,0,'PENDING','PENDING','RETURNED',  0,1, 1,0,0,0,0,0,0,0, NULL, NULL, N'ตรวจสอบและส่งต่อ'),
 (@id, 3, N'ฝ่ายการเงิน',                       0,0,0,0,0,1, 0,0,0,'PENDING','PENDING','RETURNED',  0,1, 1,0,0,0,0,0,0,0, NULL, NULL, N'ตรวจสอบและส่งต่อ'),
 (@id, 4, N'แผนกบัญชี',                         0,0,0,0,0,1, 0,0,0,'APPROVED','PENDING','RETURNED', 1,1, 1,0,0,0,0,0,0,0, NULL, NULL, N'อนุมัติจ่าย');

INSERT INTO wf_custom_user (subworkflowid, workflowid, wlevel, userid, empid, isactive, modate)
SELECT s.subworkflowid, s.workflowid, s.wlevel,
       CASE s.wlevel WHEN 2 THEN @hr WHEN 3 THEN @finance ELSE @account END,
       u.empid, 1, GETDATE()
FROM wf_sub_workflow_master s
JOIN sc_user u ON u.userid = CASE s.wlevel WHEN 2 THEN @hr WHEN 3 THEN @finance ELSE @account END
WHERE s.workflowid = @id AND s.wlevel IN (2, 3, 4);

SELECT s.wlevel, s.subject,
       s.isNeedsupervisorapprove AS [ผ่านหัวหน้ากี่ชั้น],
       s.isReturnSender AS [ส่งกลับผู้กรอกได้],
       s.istop AS [ขั้นสุดท้าย],
       u.firstname + ' ' + u.lastname AS [ผู้อนุมัติที่ระบุไว้]
FROM wf_sub_workflow_master s
LEFT JOIN wf_custom_user c ON c.subworkflowid = s.subworkflowid
LEFT JOIN sc_user u ON u.userid = c.userid
WHERE s.workflowid = @id ORDER BY s.wlevel;
