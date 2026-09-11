-- 11 ก.ย. 2569 — สถานะปิดงานของขั้นสุดท้ายต้องเป็น COMPLETED
--
-- engine ทั้งสองตัวประทับ wf_sub_workflow_master.forwardstatus ของขั้น istop ลง job_master.status
-- ตอนปิดงาน แต่โมดูลที่รอผล (Expense → payroll, IDP, Perf, Rec offer, Timesheet, StateChange)
-- เช็ค "COMPLETED" — ขั้นสุดท้ายของ wf 14 (EXPENSE_CLAIM_APPROVAL) และ 10039 (EXPENSE_TRAVEL)
-- ตั้งไว้ว่า "APPROVED" ใบเบิกที่อนุมัติแล้วจึงไม่เคยถูก push เข้า payroll
-- รันซ้ำได้: แก้เฉพาะขั้น istop ที่ forwardstatus ไม่ใช่ COMPLETED
UPDATE wf_sub_workflow_master
SET forwardstatus = 'COMPLETED'
WHERE istop = 1 AND (forwardstatus IS NULL OR forwardstatus <> 'COMPLETED');

-- งานที่ปิดด้วยการอนุมัติไปแล้วแต่สถานะยังเป็นค่าเก่า
UPDATE job_master
SET status = 'COMPLETED'
WHERE isJobClosed = 1 AND reasonClosed = 'Approve' AND status <> 'COMPLETED';
