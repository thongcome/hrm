-- ============================================================================
--  สองคอลัมน์ config บน wf_workflow
-- ============================================================================
--  1. useNewEngine — เครื่องยนต์ตัวไหนเดินงานของ workflow นี้
--
--     CEO, 10 ก.ย. 2569: "เพราะเป็น configuration base น่าจะ config แล้วใช้ได้เลย"
--
--     โมดูล 21 ตัวเรียก WorkflowEngineService ตรง ๆ ตั้งแต่ตอนเขียน การย้าย
--     มาใช้ WorkflowService จึงเคยแปลว่าต้องแก้ call site ทุกตัวแล้ว deploy
--     ตอนนี้ engine เดิมถามที่ตัว workflow เองแล้วส่งต่อ —
--     ย้าย = ติ๊ก / ถอยกลับ = ปลดติ๊ก ไม่ต้อง build ทั้งสองทาง
--     null/0 = ตัวเดิม (ค่าเริ่มต้น ของที่ใช้อยู่ไม่กระทบ)
--
--  2. doctypecode — เอกสารแนบของงานนี้อยู่ใน doc_center ใต้ doctype ไหน
--
--     CEO: "คุณต้องเอา doc_center module มาใน workflow ด้วย"
--     doc_center เก็บด้วย (doctypecode, refid) — refid ของงานคือ job_master.refid
--     ตั้งค่านี้แล้วหน้า workflow ดึงไฟล์แนบของเอกสารต้นทางมาแสดงเอง
--     ไม่ต้องรอให้หน้าโมดูลมีปุ่มแนบของตัวเอง
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

IF COL_LENGTH('wf_workflow', 'useNewEngine') IS NULL
    ALTER TABLE wf_workflow ADD useNewEngine bit NULL;
GO

IF COL_LENGTH('wf_workflow', 'doctypecode') IS NULL
    ALTER TABLE wf_workflow ADD doctypecode nvarchar(50) NULL;
GO

-- doctype ของแต่ละ workflow — อ่านจาก doctypecode ที่โมดูลใช้จริงใน doc_center
UPDATE w SET doctypecode = v.dt
FROM wf_workflow w
JOIN (VALUES
    ('LEAVE_APPROVAL',         'LEAVE_MEDCERT'),
    ('EXPENSE_TRAVEL',         'EXP_RECEIPT'),
    ('EXPENSE_CLAIM_APPROVAL', 'EXP_RECEIPT'),
    ('KM_ARTICLE_APPROVAL',    'KM_ARTICLE_ATTACHMENT'),
    ('DISCIPLINARY_APPROVAL',  'HR_DISCIPLINARY'),
    ('REWARD_APPROVAL',        'HR_REWARD'),
    ('OFFER_APPROVAL',         'CANDIDATE_RESUME'),
    ('HIRE_APPROVAL',          'CANDIDATE_RESUME')
) v(code, dt) ON v.code = w.workflowcode
WHERE w.doctypecode IS NULL;

SELECT workflowcode, tableref, doctypecode, ISNULL(useNewEngine, 0) AS useNewEngine
FROM wf_workflow
WHERE doctypecode IS NOT NULL OR useNewEngine = 1
ORDER BY workflowcode;
