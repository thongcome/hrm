-- ============================================================================
--  เหตุผลการพิจารณา — ทำให้เป็นข้อมูล ไม่ใช่ข้อความอิสระ
--
--  ข้อเสนอ BA ข้อ 2 (CEO สั่งทำทุกข้อ 10 ก.ย. 2569)
--
--  ปัญหา: หน้าอนุมัติมีช่องเลือกเหตุผลอยู่แล้ว และหน้าจัดการ /wf/reasons ก็มีแล้ว
--  แต่ mas_reason ว่างเปล่า (epms ที่ใช้จริงมี 33 แถว) ช่องเลือกจึงไม่ขึ้นเลย
--  ผู้อนุมัติพิมพ์เหตุผลเองอย่างเดียว ผลคือ:
--    - คนละคนเขียนคนละแบบ "เอกสารไม่ครบ" / "ขาดเอกสาร" / "เอกสารไม่สมบูรณ์"
--    - ตอบไม่ได้ว่าคำขอถูกตีกลับเพราะอะไรมากที่สุด ซึ่งเป็นตัวชี้ว่าแบบฟอร์มไหน
--      ออกแบบไม่ดี หรือขั้นไหนไม่จำเป็น
--
--  แถวที่ไม่ระบุ workflowid/wlevel = ใช้ได้กับทุก workflow ทุกขั้น
--  (หน้าอนุมัติหาแบบเจาะจงก่อน ไม่เจอจึงใช้แบบทั่วไป) ถ้าภายหลังอยากได้เหตุผล
--  เฉพาะขั้น เช่น "งบไม่พอ" เฉพาะขั้นการเงิน ก็เพิ่มแถวที่ระบุ workflowid+wlevel
--  ได้จากหน้า /wf/reasons โดยไม่ต้องแก้โค้ด
--
--  เลือกคำโดยดูจากชุดจริงของ epms (แนะนำ / ไม่แนะนำ / เอกสารไม่สมบูรณ์ /
--  เพื่ออนุมัติ / อนุมัติ / ไม่อนุมัติ / อื่นๆ) แล้วขยายให้ครอบคลุมเหตุผลที่ HR
--  ไทยใช้จริงทั้งฝั่งอนุมัติและฝั่งตีกลับ
--
--  รันซ้ำได้ — เพิ่มเฉพาะ code ที่ยังไม่มี ไม่แตะแถวที่ผู้ใช้แก้เอง
-- ============================================================================

SET NOCOUNT ON;
GO

MERGE mas_reason AS t
USING (VALUES
    -- ฝั่งเห็นชอบ
    ('OK_COMPLETE',   N'ข้อมูลครบถ้วน เห็นควรอนุมัติ',        N'Complete, recommended'),
    ('OK_NECESSARY',  N'มีความจำเป็นต่องาน',                   N'Justified by work need'),
    ('OK_IN_BUDGET',  N'อยู่ในงบประมาณที่ตั้งไว้',             N'Within budget'),
    ('OK_POLICY',     N'เป็นไปตามระเบียบบริษัท',               N'Complies with policy'),
    -- ฝั่งส่งกลับให้แก้
    ('BK_DOC',        N'เอกสารไม่ครบ',                          N'Missing documents'),
    ('BK_WRONG',      N'ข้อมูลไม่ถูกต้อง ต้องแก้ไข',           N'Incorrect information'),
    ('BK_DETAIL',     N'รายละเอียดไม่ชัดเจน',                   N'Insufficient detail'),
    ('BK_WRONGTYPE',  N'เลือกประเภทไม่ถูกต้อง',                N'Wrong request type'),
    ('BK_TIMING',     N'ช่วงเวลาไม่เหมาะสม ขอให้ปรับ',         N'Timing needs adjusting'),
    -- ฝั่งไม่อนุมัติ
    ('NO_QUOTA',      N'เกินสิทธิ์ที่พนักงานมี',                N'Exceeds entitlement'),
    ('NO_BUDGET',     N'งบประมาณไม่เพียงพอ',                    N'Insufficient budget'),
    ('NO_POLICY',     N'ไม่เป็นไปตามระเบียบบริษัท',            N'Against company policy'),
    ('NO_MANPOWER',   N'กระทบกำลังคนในช่วงนั้น',                N'Impacts staffing'),
    ('NO_DUP',        N'ซ้ำกับเรื่องที่ยื่นไว้แล้ว',           N'Duplicate request'),
    -- ทั่วไป
    ('OTHER',         N'อื่น ๆ (ระบุในหมายเหตุ)',               N'Other (see note)')
) AS s(code, name, name_en)
ON t.code = s.code AND t.workflowid IS NULL AND t.wlevel IS NULL
WHEN NOT MATCHED THEN
    INSERT (code, name, name_en, isActive, workflowid, wlevel, moddate, modby)
    VALUES (s.code, s.name, s.name_en, 1, NULL, NULL, GETDATE(), 'seed_reason_codes.sql');
GO

PRINT '--- เหตุผลที่ใช้ได้ทุก workflow ---';
SELECT code, name, name_en, CAST(isActive AS int) AS act
FROM   mas_reason WHERE workflowid IS NULL AND wlevel IS NULL ORDER BY code;
GO
