-- 11 ก.ย. 2569 — ปิดข้อตรวจพบ H5 / engine default จากการ audit เตรียมแยก Workflow
--
-- 1) job_master.RowVersion (rowversion) — ทุก action ของ engine เขียน job_master แถวเดียวกัน
--    (jobseq / lastLevel / status / remark) ผู้อนุมัติสองคนกดพร้อมกันที่ขั้นเดียวกัน คนที่ commit
--    ทีหลังจะได้ DbUpdateConcurrencyException แทนที่จะเขียนทับผลของคนแรก (เดิม: ทั้งคู่เห็นอีกฝ่าย
--    ยัง PENDING → ทั้งคู่ตั้ง isLast=false → งานค้าง)
-- 2) wf_workflow.useNewEngine default 1 — workflow ที่สร้างใหม่จากหน้าแอดมินไม่ได้ตั้งค่านี้ จึงตกไป
--    engine เดิมเงียบ ๆ ทั้งที่ทุก workflow ธุรกิจย้ายมา engine ใหม่แล้ว (10 ก.ย. 2569)
-- รันซ้ำได้
IF COL_LENGTH('job_master', 'RowVersion') IS NULL
    ALTER TABLE job_master ADD RowVersion rowversion NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_wf_workflow_useNewEngine')
    ALTER TABLE wf_workflow ADD CONSTRAINT DF_wf_workflow_useNewEngine DEFAULT 1 FOR useNewEngine;
GO
