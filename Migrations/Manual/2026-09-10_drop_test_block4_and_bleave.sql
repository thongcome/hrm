-- ============================================================================
--  ลบ workflow ทดสอบชุดสุดท้าย: TEST_BLOCK4_* (3 ตัว) และ Bleave
--
--  CEO, 10 ก.ย. 2569: "ทิ้งหมดเลย ทั้ง TEST_BLOCK4 กับ Bleave"
--
--  TEST_BLOCK4_ADHOC / _COSTCENTER / _RETURNSENDER — fixture ทดสอบ engine เดิม
--  (Block 4) แต่ละตัวมี 1 ขั้นที่ isshow=0 ไม่มีงานเลย ไม่มีโมดูลไหนเรียก
--  Bleave — 0 ขั้น เริ่มงานไม่ได้ตั้งแต่แรกทั้งสอง engine
--
--  หลังสคริปต์นี้ workflow ที่เหลือทั้งหมดเป็นงานจริง (25 ตัวบน engine ใหม่ +
--  WELFARE_CLAIM ที่รอปิดงาน 10089 แล้วย้ายตาม)
--
--  ลำดับ FK ชุดเดียวกับ 2026-09-10_drop_demo_workflows.sql (อ่านจาก sys.foreign_keys
--  จริง ไม่เดา) ทำในธุรกรรมเดียว ติดตรงไหนถอยกลับทั้งหมด — รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('tempdb..#doomed') IS NOT NULL DROP TABLE #doomed;
SELECT workflowid, workflowcode INTO #doomed
FROM   wf_workflow
WHERE  workflowcode LIKE 'TEST[_]BLOCK4[_]%' OR workflowcode = 'Bleave';

IF OBJECT_ID('tempdb..#doomedJobs') IS NOT NULL DROP TABLE #doomedJobs;
SELECT jobmasterid INTO #doomedJobs
FROM   job_master WHERE workflowid IN (SELECT workflowid FROM #doomed);

PRINT '--- กำลังจะลบ ---';
SELECT workflowcode FROM #doomed ORDER BY workflowcode;
SELECT (SELECT COUNT(*) FROM #doomed) AS workflows, (SELECT COUNT(*) FROM #doomedJobs) AS jobs;
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

    -- ปุ่มที่ config เจาะจง workflow เหล่านี้ (ชุดกลาง workflowid NULL ไม่ถูกแตะ)
    DELETE FROM wf_button   WHERE workflowid IN (SELECT workflowid FROM #doomed);

    DELETE FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM #doomed);
    DELETE FROM wf_workflow            WHERE workflowid IN (SELECT workflowid FROM #doomed);

COMMIT TRANSACTION;
GO

PRINT '--- workflow ทดสอบที่ยังเหลือ (ควรว่าง) ---';
SELECT workflowcode FROM wf_workflow
WHERE  workflowcode LIKE 'TEST[_]%' OR workflowcode LIKE 'DEMO[_]%' OR workflowcode IN ('T_STEP', 'Bleave');

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
SELECT workflowcode, ISNULL(CAST(useNewEngine AS int), 0) AS newEngine,
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid AND s.isshow = 1) AS levels
FROM   wf_workflow w ORDER BY workflowcode;
GO

DROP TABLE #doomed;
DROP TABLE #doomedJobs;
GO
