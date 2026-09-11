-- 12 ก.ย. 2569 — ปลด engine เก่าของ workflow (CEO: "ทำให้ครบเลย" ข้อ 6)
--   1) งานทดสอบ 10089 (WELFARE_CLAIM / Wel_Claim/1) ค้างอยู่บน engine เก่าตัวเดียว → ปิดเป็นยกเลิก
--   2) ทุก workflow ที่มีขั้น → useNewEngine = 1 (โค้ดหลังจากนี้เดินทางเดียว: WorkflowService)
-- รันซ้ำได้
SET NOCOUNT ON;
GO
UPDATE job_master SET isJobClosed = 1, status = 'CANCELLED', reasonClosed = N'ปิดงานทดสอบก่อนปลด engine เก่า (12 ก.ย. 2569)'
WHERE jobmasterid = 10089 AND ISNULL(isJobClosed, 0) = 0;
UPDATE job_user_list SET jobstatus = 'CANCELLED' WHERE jobmasterid = 10089 AND jobstatus = 'PENDING';
GO
UPDATE wf_workflow SET useNewEngine = 1 WHERE ISNULL(useNewEngine, 0) = 0;
GO
PRINT '--- engines now';
SELECT CAST(COUNT(*) AS varchar) + ' workflows, old-engine=' + CAST(SUM(CASE WHEN ISNULL(useNewEngine,0)=0 THEN 1 ELSE 0 END) AS varchar) FROM wf_workflow;
SELECT 'open jobs on old-engine workflows: ' + CAST(COUNT(*) AS varchar) FROM job_master j JOIN wf_workflow w ON w.workflowid = j.workflowid WHERE ISNULL(j.isJobClosed,0)=0 AND ISNULL(w.useNewEngine,0)=0;
GO
