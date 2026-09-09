-- ============================================================================
--  T_STEP — workflow ทดสอบการเดินงานตามแบบของ CEO (4 ขั้น เริ่มที่ 0)
-- ============================================================================
--  ใช้ทดสอบ WorkflowService ตัวใหม่ให้ครบวง:
--    สร้างงาน -> ขั้น 0 (ผู้ขอถือ draft)
--    ส่งต่อ 0->1 -> ส่งกลับ 1->0 -> ส่งต่อ 0->1->2
--    ที่ขั้น 2 กด "ส่งกลับหาผู้กรอกแบบฟอร์ม" (isReturnSender) -> กลับถึงขั้น 0 เลย
--    ส่งต่อจนถึงขั้น 3 (istop) -> Approve = จบงาน
--
--  ผู้อนุมัติทุกขั้นตั้งเป็น KB0001 (userid 7033) เพื่อให้ทดสอบคนเดียวได้ครบวง
--  ขั้น 0 ไม่ต้อง config ผู้เกี่ยวข้อง — ผู้สร้างงานคือคนถือ draft โดยนิยาม
-- ============================================================================

SET NOCOUNT ON;

DECLARE @uid bigint = (SELECT userid FROM sc_user WHERE loginname = 'KB0001');
IF @uid IS NULL BEGIN RAISERROR('ไม่พบผู้ใช้ KB0001',16,1); RETURN; END

-- ลบของเดิมทิ้งก่อน (สคริปต์นี้รันซ้ำได้)
DECLARE @old bigint = (SELECT workflowid FROM wf_workflow WHERE workflowcode = 'T_STEP');
IF @old IS NOT NULL
BEGIN
    DELETE FROM wf_custom_user           WHERE workflowid = @old;
    DELETE FROM wf_sub_workflow_master   WHERE workflowid = @old;
    DELETE FROM wf_workflow              WHERE workflowid = @old;
END

DECLARE @id bigint = (SELECT ISNULL(MAX(workflowid),0) + 1 FROM wf_workflow);
SET IDENTITY_INSERT wf_workflow ON;
INSERT INTO wf_workflow (workflowid, wname, wstatus, isshow, isactive, workflowcode)
VALUES (@id, N'ทดสอบการเดินงาน 3 ขั้น', N'ACTIVE', 1, 1, 'T_STEP');
SET IDENTITY_INSERT wf_workflow OFF;

INSERT INTO wf_sub_workflow_master
 (workflowid, wlevel, subject, isAdhocUser, iscustomApprover, isupperrole, isupperuser, iscustomRole, iscustomUser,
  iscondition, isorcondition, isandcondition, forwardstatus, standstatus, backwardstatus, istop, isReturnSender,
  isshow, isLOA, isAutoApproveAllow, isNeedBudgetApproval, isPool, isApproverSameOrg, isApproverSameCostCenter,
  isManualButton, sitinstatus, backwardlevel, displayName)
VALUES
 -- ขั้น 0 = ขั้นของผู้ขอ (draft) ไม่ต้องติ๊กอะไร ผู้สร้างงานคือคนถือ
 (@id, 0, N'ผู้ขอ (draft)',            0,0,0,0,0,0, 0,0,0, 'PENDING','DRAFT','RETURNED',    0, 0, 1,0,0,0,0,0,0,0, 'DRAFT', NULL, N'ส่งขออนุมัติ'),
 (@id, 1, N'ขั้นที่ 1',                 0,0,0,0,0,1, 0,0,0, 'PENDING','PENDING','RETURNED',  0, 0, 1,0,0,0,0,0,0,0, NULL,    NULL, NULL),
 -- ขั้น 2 ให้สิทธิ์ส่งกลับถึงผู้กรอกแบบฟอร์มได้เลย (ข้ามกลับหลายขั้น + แจ้งทุกคน)
 (@id, 2, N'ขั้นที่ 2 (ส่งกลับผู้ขอได้)', 0,0,0,0,0,1, 0,0,0, 'PENDING','PENDING','RETURNED',  0, 1, 1,0,0,0,0,0,0,0, NULL,    1,    NULL),
 -- ขั้น 3 = ขั้นสุดท้าย อนุมัติที่นี่คือจบงาน (ไม่มีที่ให้ไปต่อ)
 (@id, 3, N'ขั้นที่ 3 (สุดท้าย)',        0,0,0,0,0,1, 0,0,0, 'APPROVED','PENDING','RETURNED', 1, 0, 1,0,0,0,0,0,0,0, NULL,    NULL, N'อนุมัติ');

INSERT INTO wf_custom_user (subworkflowid, workflowid, wlevel, userid, isactive)
SELECT s.subworkflowid, s.workflowid, s.wlevel, @uid, 1
FROM wf_sub_workflow_master s
WHERE s.workflowid = @id AND s.wlevel > 0;

SELECT s.wlevel, s.subject, s.iscustomUser, s.isReturnSender, s.istop, s.backwardlevel
FROM wf_sub_workflow_master s WHERE s.workflowid = @id ORDER BY s.wlevel;
