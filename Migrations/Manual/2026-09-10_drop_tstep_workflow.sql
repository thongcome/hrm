-- ============================================================================
--  ลบ workflow ทดสอบ T_STEP
--
--  CEO, 10 ก.ย. 2569: "T_STEP ทิ้งด้วยเลย" (ต่อจาก "demo* ทิ้งหมดเลยครับ")
--
--  T_STEP คือ workflow ที่ใช้เดินวงเต็มตอนพัฒนา engine ใหม่ (create -> submit ->
--  ส่งกลับทีละขั้น -> ส่งกลับหาผู้ยื่น -> อนุมัติที่ istop) ตั้งผู้อนุมัติเป็น
--  KB0001/AD0002/AD0003 และมี config ระดับ 0 ค้างอยู่ ซึ่งขัดกับกฎที่ตกลงกันว่า
--  ระดับ 0 คือ draft ไม่ต้อง config — ไม่ใช่ของงานจริง ไม่มีโมดูลไหนเรียก
--
--  ที่หายไปด้วยและรับรู้ไว้: หลังลบตัวนี้ ในฐานข้อมูลจะไม่เหลือ workflow ที่
--  เดินวงทดสอบได้โดยไม่กระทบงานจริงอีก ถ้าจะทดสอบ engine อีกครั้งต้องสร้างใหม่
--  จากหน้าออกแบบ workflow หรือใช้ workflow งานจริงบนข้อมูลทดสอบ
--
--  ใช้ลำดับ FK ชุดเดียวกับที่พิสูจน์แล้วตอนลบ DEMO_* (2026-09-10_drop_demo_workflows.sql)
--  ทำในธุรกรรมเดียว ติดตรงไหนถอยกลับทั้งหมด
--
--  ไม่แตะ AuditLog — ถูกล้างไปแล้วทั้งตารางตามคำสั่ง CEO เรื่อง dev mode
--  (2026-09-10_clear_dev_auditlog.sql)
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('tempdb..#doomed') IS NOT NULL DROP TABLE #doomed;
SELECT workflowid INTO #doomed FROM wf_workflow WHERE workflowcode = 'T_STEP';

IF OBJECT_ID('tempdb..#doomedJobs') IS NOT NULL DROP TABLE #doomedJobs;
SELECT jobmasterid INTO #doomedJobs
FROM   job_master WHERE workflowid IN (SELECT workflowid FROM #doomed);

PRINT '--- กำลังจะลบ ---';
SELECT (SELECT COUNT(*) FROM #doomed)     AS workflows,
       (SELECT COUNT(*) FROM #doomedJobs) AS jobs;
GO

BEGIN TRANSACTION;

    DELETE FROM job_user_list          WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM wf_adhoc_user          WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM job_subworkflow_master WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM job_loa                WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);
    DELETE FROM job_master             WHERE jobmasterid IN (SELECT jobmasterid FROM #doomedJobs);

    DELETE FROM wf_custom_user     WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_custom_role     WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_budget          WHERE subworkflowid IN
        (SELECT subworkflowid FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed));
    DELETE FROM wf_decision_status WHERE subworkflowid IN
        (SELECT subworkflowid FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed));

    DELETE FROM wf_loa_user WHERE loaid IN
        (SELECT id FROM wf_loa WHERE wfid IN (SELECT workflowid FROM #doomed));
    DELETE FROM wf_loa      WHERE wfid IN (SELECT workflowid FROM #doomed);
    DELETE FROM mas_reason  WHERE workflowid IN (SELECT workflowid FROM #doomed);

    DELETE FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_workflow            WHERE workflowid IN (SELECT workflowid FROM #doomed);

COMMIT TRANSACTION;
GO

PRINT '--- workflow ที่ยังมีชื่อคนตายตัว (ควรเหลือแต่ EXPENSE_TRAVEL) ---';
SELECT DISTINCT w.workflowcode
FROM   wf_custom_user c JOIN wf_workflow w ON w.workflowid = c.workflowid
ORDER  BY w.workflowcode;

PRINT '--- workflow ที่ยังมี config ระดับ 0 (ควรไม่เหลือ) ---';
SELECT w.workflowcode
FROM   wf_workflow w
WHERE  EXISTS (SELECT 1 FROM wf_sub_workflow_master s
               WHERE s.workflowid = w.workflowid AND s.wlevel = 0);

PRINT '--- แถวกำพร้า (ควรเป็น 0 ทุกช่อง) ---';
SELECT
  (SELECT COUNT(*) FROM wf_sub_workflow_master s
   WHERE NOT EXISTS (SELECT 1 FROM wf_workflow w WHERE w.workflowid = s.workflowid)) AS orphanLevels,
  (SELECT COUNT(*) FROM job_master j
   WHERE NOT EXISTS (SELECT 1 FROM wf_workflow w WHERE w.workflowid = j.workflowid)) AS orphanJobs,
  (SELECT COUNT(*) FROM job_user_list u
   WHERE NOT EXISTS (SELECT 1 FROM job_master j WHERE j.jobmasterid = u.jobmasterid)) AS orphanApproverRows,
  (SELECT COUNT(*) FROM job_subworkflow_master s
   WHERE NOT EXISTS (SELECT 1 FROM job_master j WHERE j.jobmasterid = s.jobmasterid)) AS orphanFootprints;

PRINT '--- workflow ที่เหลือทั้งหมด ---';
SELECT w.workflowcode, ISNULL(CAST(w.useNewEngine AS int),0) AS newEngine,
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid=w.workflowid AND s.isshow=1) AS levels
FROM   wf_workflow w ORDER BY w.workflowcode;
GO

DROP TABLE #doomed;
DROP TABLE #doomedJobs;
GO
