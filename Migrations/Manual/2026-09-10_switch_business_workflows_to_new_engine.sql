-- ============================================================================
--  ย้าย workflow ของงานจริงมาใช้ engine ใหม่ (WorkflowService)
--
--  CEO, 10 ก.ย. 2569: "เปลี่ยน workflow ทั้งหมด มาเป็นอันนี้"
--
--  ย้าย = ติ๊ก wf_workflow.useNewEngine เท่านั้น ไม่ต้อง build ไม่ต้องแก้ call site
--  (WorkflowEngineService.StartJobAsync ถาม flag นี้แล้วส่งต่อให้ engine ใหม่เอง)
--  ถอยกลับ = ปลดติ๊ก
--
--  ────────────────────────────────────────────────────────────────────────────
--  เกณฑ์ที่ "ยัง" ไม่ย้าย และเหตุผล — สคริปต์นี้รันซ้ำได้ พอเงื่อนไขหมดไป
--  รันอีกครั้งมันจะย้ายให้เอง ไม่ต้องมาแก้ลิสต์ด้วยมือ
--
--  1) workflow ทดสอบ/สาธิต (DEMO_*, TEST_*, T_STEP)
--     ไม่ใช่งานจริง และหลายตัวตั้งใจ config ให้ทดสอบความสามารถของ engine เดิม
--     โดยเฉพาะ (DEMO_LOA ใช้ wf_loa กระโดดข้ามขั้น ซึ่ง engine ใหม่ยังไม่ทำ,
--     DEMO_BOUNCE/DEMO_STALE_JOBSEQ ใช้ backwardlevel) ย้ายไปแล้วเทสเดิมจะ
--     ทดสอบสิ่งที่มันตั้งใจทดสอบไม่ได้ — ปล่อยไว้ที่ engine เดิมตามเจตนา
--     T_STEP ยังมี config ระดับ 0 อยู่ ซึ่งขัดกับกฎ "ระดับ 0 คือ draft
--     ไม่ต้อง config" (CEO, 10 ก.ย. 2569) จึงยังไม่ย้ายจนกว่าจะจัดการ config
--
--  2) workflow ที่ยังไม่มีขั้นเลย (Bleave — 0 ระดับ)
--     ไม่มี config ให้เดิน เปิดใช้ก็เริ่มงานไม่ได้ทั้งสอง engine
--
--  3) workflow ที่ยังมีงานค้างเดินอยู่
--     ระหว่างงานเดิน หน้ารายละเอียดเลือกจาก useNewEngine ของ workflow
--     ถ้าสลับกลางทาง งานที่เริ่มด้วย engine เดิมจะไปเปิดหน้าของ engine ใหม่
--     ตารางเดียวกันก็จริง แต่ความหมายของ jobseq ต่างกัน (engine ใหม่เดินทุก
--     action) ไม่เอาความเสี่ยงนั้นกับงานที่กำลังเดิน — รอปิดงานแล้วรันซ้ำ
--
--  ────────────────────────────────────────────────────────────────────────────
--  หมายเหตุที่พบตอนตรวจ config (ไม่ได้แก้ในสคริปต์นี้ แค่รายงาน):
--  - OT_APPROVAL ระดับ 2 ติ๊ก isLOA=1 แต่ไม่มีแถบวงเงินใน wf_loa เลย
--    engine เดิมจะหาผู้อนุมัติไม่ได้และโยน exception, engine ใหม่คืนค่าว่าง
--    พร้อมเหตุผลแล้วไปใช้ iscustomUser ของระดับนั้นต่อ — การย้ายจึงทำให้
--    workflow นี้ "ทำงานได้" ขึ้น ไม่ใช่แย่ลง (ยังไม่เคยมีงาน OT จริงเลย 0 งาน)
--  - wf_loa id 5 เป็นแถวขยะ (wfid ไม่ตรงกับ workflow ไหน ค่าอื่นว่างหมด)
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

IF COL_LENGTH('wf_workflow', 'useNewEngine') IS NULL
    ALTER TABLE wf_workflow ADD useNewEngine bit NULL;
GO

UPDATE w
SET    useNewEngine = 1
FROM   wf_workflow w
WHERE  ISNULL(w.useNewEngine, 0) = 0
  -- ไม่ใช่ workflow ทดสอบ/สาธิต
  AND  w.workflowcode NOT LIKE 'DEMO[_]%'
  AND  w.workflowcode NOT LIKE 'TEST[_]%'
  AND  w.workflowcode <> 'T_STEP'
  -- ต้องมีขั้นให้เดิน และมีปลายทางเดียวชัดเจน
  AND  EXISTS (SELECT 1 FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid)
  AND  (SELECT COUNT(*) FROM wf_sub_workflow_master s
        WHERE s.workflowid = w.workflowid AND s.istop = 1) = 1
  -- ระดับ 0 คือ draft ไม่ต้อง config — ถ้ายังมีอยู่ config ยังไม่ถูก
  AND  NOT EXISTS (SELECT 1 FROM wf_sub_workflow_master s
                   WHERE s.workflowid = w.workflowid AND s.wlevel = 0)
  -- ไม่สลับ engine ให้ workflow ที่มีงานกำลังเดินอยู่
  AND  NOT EXISTS (SELECT 1 FROM job_master j
                   WHERE j.workflowid = w.workflowid AND ISNULL(j.isJobClosed, 0) = 0);
GO

PRINT '--- ผลลัพธ์: workflow ไหนอยู่ engine ไหน ---';
SELECT w.workflowcode,
       ISNULL(CAST(w.useNewEngine AS int), 0)                                   AS useNewEngine,
       (SELECT COUNT(*) FROM wf_sub_workflow_master s WHERE s.workflowid = w.workflowid) AS levels,
       (SELECT COUNT(*) FROM job_master j
        WHERE j.workflowid = w.workflowid AND ISNULL(j.isJobClosed, 0) = 0)     AS openJobs,
       CASE
         WHEN ISNULL(w.useNewEngine, 0) = 1 THEN ''
         WHEN w.workflowcode LIKE 'DEMO[_]%'
           OR w.workflowcode LIKE 'TEST[_]%'
           OR w.workflowcode = 'T_STEP'                       THEN 'workflow ทดสอบ'
         WHEN NOT EXISTS (SELECT 1 FROM wf_sub_workflow_master s
                          WHERE s.workflowid = w.workflowid)  THEN 'ยังไม่มีขั้นเลย'
         WHEN (SELECT COUNT(*) FROM wf_sub_workflow_master s
               WHERE s.workflowid = w.workflowid AND s.istop = 1) <> 1
                                                              THEN 'ไม่มี istop เดียวชัดเจน'
         WHEN EXISTS (SELECT 1 FROM wf_sub_workflow_master s
                      WHERE s.workflowid = w.workflowid AND s.wlevel = 0)
                                                              THEN 'ยังมี config ระดับ 0'
         WHEN EXISTS (SELECT 1 FROM job_master j
                      WHERE j.workflowid = w.workflowid AND ISNULL(j.isJobClosed, 0) = 0)
                                                              THEN 'มีงานค้างเดินอยู่ รอปิดแล้วรันซ้ำ'
         ELSE 'ไม่ทราบสาเหตุ'
       END AS reasonNotMoved
FROM   wf_workflow w
ORDER  BY ISNULL(CAST(w.useNewEngine AS int), 0) DESC, w.workflowcode;
GO
