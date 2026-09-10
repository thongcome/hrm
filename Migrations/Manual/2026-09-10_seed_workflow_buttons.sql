-- ============================================================================
--  ปุ่มบนหน้าอนุมัติ — ย้ายจากโค้ดมาเป็น config
--
--  CEO, 10 ก.ย. 2569: "เพิ่มปุ่ม ไม่อนุมัติ -> config ใน table ได้เลย ดูจาก epms.workflow"
--
--  epms เลือกปุ่มด้วย WorkflowServices.GetButtonList(isStart, istop, isAndCondition)
--      db.wf_button.Where(isStart == x && istop == y && isAndCondition == z && isactive)
--                  .OrderBy(wf_button_master.orderth)
--  ไม่มีเงื่อนไขว่าต้องติ๊กอะไรก่อน — อ่านตารางเสมอ
--  ของเราเดิมกั้นด้วย isManualButton ตารางปุ่มจึงไม่เคยถูกใช้เลย (0 แถว)
--
--  ชุดปุ่มจริงของ epms ใน production (ttmepms.wf_button 12 แถว):
--      ขั้นกลาง (istop=0)  : Submit, Reject, ApprovePartial, DeclinePartial
--      ขั้นสุดท้าย (istop=1): Approve, Decline, Reject
--  เราทำตามโครงเดียวกัน ยกเว้น ApprovePartial/DeclinePartial ที่ยังไม่มีเมธอด
--  รองรับ — ไม่ใส่ปุ่มที่กดแล้วไม่มีอะไรเกิดขึ้น
--
--  แถวที่ workflowid เป็น NULL = ใช้กับทุก workflow (ชุดกลาง)
--  ถ้าภายหลังอยาก override เฉพาะ workflow/ขั้นไหน ก็เพิ่มแถวที่ระบุ
--  workflowid + wlevel ซึ่ง service จะเลือกใช้ก่อนชุดกลางเสมอ
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

-- ── 1. นิยามปุ่ม (wf_button_master) ─────────────────────────────────────────
--    ของเดิม seed ไว้ 3 ปุ่มเป็นภาษาอังกฤษ ปรับเป็นคำที่ผู้ใช้เห็นจริงบนหน้าจอ
--    และเพิ่ม "ส่งต่อ" ซึ่งเป็นปุ่มของขั้นกลาง (epms แยก Submit ออกจาก Approve)
MERGE wf_button_master AS t
USING (VALUES
    ('submit',  N'ส่งต่อ',     'btn btn-primary', 'submit',  1),
    ('approve', N'อนุมัติ',    'btn btn-success', 'approve', 2),
    ('reject',  N'ส่งกลับ',    'btn btn-warning', 'reject',  3),
    ('decline', N'ไม่อนุมัติ', 'btn btn-danger',  'decline', 4)
) AS s(code, label, class_style, actiontypecode, orderth)
ON t.code = s.code
WHEN MATCHED THEN UPDATE SET
    t.name = s.label, t.value = s.label, t.class_style = s.class_style,
    t.actiontypecode = s.actiontypecode, t.orderth = s.orderth,
    t.moddate = GETDATE(), t.modby = 'seed_workflow_buttons.sql'
WHEN NOT MATCHED THEN
    INSERT (name, code, value, class_style, actiontypecode, btnType, orderth, moddate, modby)
    VALUES (s.label, s.code, s.label, s.class_style, s.actiontypecode, 'submit', s.orderth,
            GETDATE(), 'seed_workflow_buttons.sql');
GO

-- ── 2. ปุ่มไหนขึ้นที่ขั้นแบบไหน (wf_button) ─────────────────────────────────
--    isAndCondition ต้องมีทั้งสองค่า เพราะ service กรองตรง ๆ ตาม epms
--    ถ้าใส่แค่ค่าเดียว ขั้นที่ตั้ง AND% ไว้จะหาปุ่มไม่เจอแล้วตกไปใช้ปุ่มในโค้ด
IF OBJECT_ID('tempdb..#want') IS NOT NULL DROP TABLE #want;
CREATE TABLE #want (code nvarchar(50), istop bit, isAndCondition bit);

INSERT INTO #want (code, istop, isAndCondition) VALUES
    -- ขั้นกลาง: ส่งต่อ หรือ ส่งกลับ — ปฏิเสธถาวรไม่ได้ เพราะยังไม่ใช่ผู้ตัดสินสุดท้าย
    ('submit',  0, 0), ('reject',  0, 0),
    ('submit',  0, 1), ('reject',  0, 1),
    -- ขั้นสุดท้าย: อนุมัติ ส่งกลับ หรือไม่อนุมัติ
    ('approve', 1, 0), ('reject',  1, 0), ('decline', 1, 0),
    ('approve', 1, 1), ('reject',  1, 1), ('decline', 1, 1);

INSERT INTO wf_button (btname, bcode, class_style, isactive, isshow, istop, isStart,
                       isAndCondition, button_masterid, workflowid, wlevel)
SELECT m.value, m.code, m.class_style, 1, 1, w.istop, 0,
       w.isAndCondition, m.id, NULL, NULL
FROM   #want w
JOIN   wf_button_master m ON m.code = w.code
WHERE  NOT EXISTS (
    SELECT 1 FROM wf_button b
    WHERE b.button_masterid = m.id AND b.workflowid IS NULL AND b.wlevel IS NULL
      AND ISNULL(b.istop, 0) = w.istop AND b.isAndCondition = w.isAndCondition);
GO

PRINT '--- ปุ่มที่จะขึ้นในแต่ละแบบของขั้น ---';
SELECT CASE WHEN ISNULL(b.istop,0) = 1 THEN N'ขั้นสุดท้าย' ELSE N'ขั้นกลาง' END AS levelKind,
       CASE WHEN b.isAndCondition = 1 THEN N'AND%' ELSE N'ปกติ' END           AS condKind,
       m.orderth, m.code, m.value, m.actiontypecode, CAST(b.isactive AS int) AS act
FROM   wf_button b JOIN wf_button_master m ON m.id = b.button_masterid
WHERE  b.workflowid IS NULL
ORDER  BY b.istop, b.isAndCondition, m.orderth;
GO

DROP TABLE #want;
GO
