-- ============================================================================
--  ลบ workflow สาธิต DEMO_* ทิ้งทั้งหมด
--
--  CEO, 10 ก.ย. 2569: "demo* ทิ้งหมดเลยครับ"
--
--  DEMO_* 10 ตัวถูกสร้างไว้ตอนพัฒนา engine เพื่อทดสอบความสามารถทีละอย่าง
--  (AND%, OR, LOA, bounce, auto-skip, vertical, mix, multi-user, stale jobseq)
--  ทุกตัวตั้งผู้อนุมัติเป็น admin/advadmin ซึ่งเป็น account ของผู้พัฒนา
--  ไม่ใช่ config ของงานจริง และไม่มีโมดูลไหนเรียกใช้
--
--  ที่หายไปด้วยและรับรู้ไว้: DEMO_LOA เป็นที่เดียวในฐานข้อมูลที่มีแถบวงเงิน
--  wf_loa จริง และ DEMO_BOUNCE/DEMO_STALE_JOBSEQ เป็นที่เดียวที่ใช้ backwardlevel
--  ถ้าจะทดสอบสองอย่างนี้อีกต้อง config ขึ้นใหม่ (สร้างจากหน้าออกแบบ workflow ได้)
--
--  ไม่แตะ AuditLog — ไม่มี FK ผูกกับตารางพวกนี้ และเป็นข้อมูลที่ต้องเก็บ
--  ไม่น้อยกว่า 90 วันตาม พ.ร.บ.คอมพิวเตอร์ ประวัติว่าเคยมีงานทดสอบอะไรจึงยังอยู่
--
--  ลบตามลำดับ FK ที่ตรวจจากฐานข้อมูลจริง (sys.foreign_keys):
--    job_master        <- job_user_list, wf_adhoc_user
--    wf_sub_workflow_master <- job_user_list, wf_budget, wf_custom_role,
--                              wf_custom_user, wf_decision_status
--    wf_workflow       <- job_master
--    (job_subworkflow_master / job_loa / mas_reason ไม่มี FK แต่ก็ต้องเก็บกวาด)
--
--  ทำในธุรกรรมเดียว — ถ้าติด FK ที่ไหน ถอยกลับทั้งหมด ไม่ทิ้งสภาพครึ่ง ๆ
--  รันซ้ำได้ (รอบสองไม่เจออะไรให้ลบ)
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('tempdb..#doomed') IS NOT NULL DROP TABLE #doomed;
SELECT workflowid INTO #doomed FROM wf_workflow WHERE workflowcode LIKE 'DEMO[_]%';

IF OBJECT_ID('tempdb..#doomedJobs') IS NOT NULL DROP TABLE #doomedJobs;
SELECT jobmasterid INTO #doomedJobs
FROM   job_master WHERE workflowid IN (SELECT workflowid FROM #doomed);

PRINT '--- กำลังจะลบ ---';
SELECT (SELECT COUNT(*) FROM #doomed)     AS workflows,
       (SELECT COUNT(*) FROM #doomedJobs) AS jobs;
GO

BEGIN TRANSACTION;

    -- ลูกของ job_master
    DELETE FROM job_user_list  WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM wf_adhoc_user  WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);

    -- ไม่มี FK แต่เป็นข้อมูลของงานเดียวกัน
    DELETE FROM job_subworkflow_master WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM job_loa                WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);

    DELETE FROM job_master WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);

    -- ลูกของ wf_sub_workflow_master
    DELETE FROM wf_custom_user     WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_custom_role     WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_budget          WHERE subworkflowid IN
        (SELECT subworkflowid FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed));
    DELETE FROM wf_decision_status WHERE subworkflowid IN
        (SELECT subworkflowid FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed));

    -- แถบวงเงินและผู้อนุมัติตามวงเงินของ workflow เหล่านี้
    DELETE FROM wf_loa_user WHERE loaid IN
        (SELECT id FROM wf_loa WHERE wfid IN (SELECT workflowid FROM #doomed));
    DELETE FROM wf_loa      WHERE wfid IN (SELECT workflowid FROM #doomed);

    -- เหตุผลสำเร็จรูปที่ผูกกับ workflow เหล่านี้
    DELETE FROM mas_reason  WHERE workflowid IN (SELECT workflowid FROM #doomed);

    DELETE FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_workflow            WHERE workflowid IN (SELECT workflowid FROM #doomed);

COMMIT TRANSACTION;
GO

PRINT '--- เหลืออะไรอยู่ ---';
SELECT COUNT(*) AS demoWorkflowsLeft FROM wf_workflow WHERE workflowcode LIKE 'DEMO[_]%';

PRINT '--- workflow ที่ยังมีชื่อคนตายตัว (ควรเหลือแต่ EXPENSE_TRAVEL กับ T_STEP) ---';
SELECT DISTINCT w.workflowcode
FROM   wf_custom_user c JOIN wf_workflow w ON w.workflowid = c.workflowid
ORDER  BY w.workflowcode;

PRINT '--- แถวกำพร้าที่ชี้ไป workflow ที่ไม่มีแล้ว (ควรเป็น 0 ทุกช่อง) ---';
SELECT
  (SELECT COUNT(*) FROM wf_sub_workflow_master s
   WHERE NOT EXISTS (SELECT 1 FROM wf_workflow w WHERE w.workflowid = s.workflowid)) AS orphanLevels,
  (SELECT COUNT(*) FROM job_master j
   WHERE NOT EXISTS (SELECT 1 FROM wf_workflow w WHERE w.workflowid = j.workflowid)) AS orphanJobs,
  (SELECT COUNT(*) FROM job_user_list u
   WHERE NOT EXISTS (SELECT 1 FROM job_master j WHERE j.jobmasterid = u.jobmasterid)) AS orphanApproverRows,
  (SELECT COUNT(*) FROM job_subworkflow_master s
   WHERE NOT EXISTS (SELECT 1 FROM job_master j WHERE j.jobmasterid = s.jobmasterid)) AS orphanFootprints;
GO

DROP TABLE #doomed;
DROP TABLE #doomedJobs;
GO
