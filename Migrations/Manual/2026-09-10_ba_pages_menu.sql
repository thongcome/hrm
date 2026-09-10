-- ============================================================================
--  เมนูของ 3 หน้าใหม่จากข้อเสนอ BA (CEO: "เพิ่มเมนู 3 หน้าใหม่เลย")
--
--    /wf/delegations  มอบฉันทะการอนุมัติ  — บริการตัวเอง ทุกคนต้องเห็น
--    /wf/health       ตรวจสุขภาพ Workflow — ผู้ดูแล
--    /wf/cycle-time   เวลาที่ใช้ในแต่ละขั้น — ผู้ดูแล
--
--  เมนูของระบบนี้อ่านจาก sc_menu + sc_role_menu ไม่ได้ hardcode ใน layout
--  ไม่มีแถวเมนู = เข้าได้เฉพาะพิมพ์ URL เอง
--
--  ธรรมเนียมที่ตรวจจากข้อมูลจริง (10 ก.ย. 2569):
--    - กลุ่ม GRP_WF_ENGINE เห็นเฉพาะ role admin
--    - หน้าที่พนักงานทุกคนต้องเห็น (เช่น /wf/my-inbox) มีแถวซ้ำอีกใบใน GRP_ESS
--      และแถวนั้นถูก grant ให้ role ที่เห็น ESS — ทำตามแบบเดียวกันกับมอบฉันทะ
--    - หน้าที่ติด [Authorize(Policy = "Menu:WF_WORKFLOW_ADMIN")] ต้องใส่ menucode
--      ให้ตรง ไม่งั้น claim ไม่ผูก แล้วหน้าขึ้น Access denied ทั้งที่ให้สิทธิ์แล้ว
--    - url ซ้ำได้ข้ามกลุ่ม การเช็ค add-missing จึงต้องดูคู่ (url, uppermenucode)
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

-- ── helper: เพิ่มเมนูหนึ่งแถวในกลุ่ม แล้ว grant role ตามแถวอ้างอิง ─────────────
IF OBJECT_ID('tempdb..#AddMenu') IS NOT NULL DROP PROCEDURE #AddMenu;
GO
CREATE PROCEDURE #AddMenu
    @url        nvarchar(400),
    @group      nvarchar(100),
    @name       nvarchar(200),
    @nameEn     nvarchar(200),
    @order      int,
    @menucode   nvarchar(100) = NULL,
    @grantLike  bigint        = NULL   -- copy role grants จาก menuid นี้ (NULL = ทุก role ที่เห็นกลุ่ม)
AS
BEGIN
    IF EXISTS (SELECT 1 FROM sc_menu
               WHERE CAST(url AS nvarchar(400)) = @url AND uppermenucode = @group)
        RETURN;

    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = @group);
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);

    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder,
                         menucode, uppermenucode, isshow, url, isactive, menugroupid, moddate)
    VALUES (@id, @name, @nameEn, 2, 1, @order, @menucode, @group, 1, @url, 1, @grp, GETDATE());
    SET IDENTITY_INSERT sc_menu OFF;

    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT rm.roleid, @id, 1, GETDATE()
    FROM   sc_role_menu rm
    WHERE  (   (@grantLike IS NOT NULL AND rm.menuid = @grantLike)
            OR (@grantLike IS NULL AND rm.menuid IN (SELECT menuid FROM sc_menu WHERE uppermenucode = @group)) )
      AND  NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = rm.roleid AND x.menuid = @id);
END
GO

-- ── 1. มอบฉันทะ — ทั้งในกลุ่ม Workflow (admin) และใน ESS (พนักงานทุกคน) ────────
DECLARE @essInbox bigint = (SELECT TOP 1 menuid FROM sc_menu
                            WHERE CAST(url AS nvarchar(400)) = '/wf/my-inbox' AND uppermenucode = 'GRP_ESS');

EXEC #AddMenu '/wf/delegations', 'GRP_WF_ENGINE', N'มอบฉันทะการอนุมัติ', 'Approval Delegation', 103;
EXEC #AddMenu '/wf/delegations', 'GRP_ESS',       N'มอบฉันทะการอนุมัติ', 'Approval Delegation', 78, NULL, @essInbox;   -- ต่อจาก Pool (77)

-- ── 2. ตรวจสุขภาพ — อยู่ติดกับหน้าออกแบบ เพราะใช้คู่กันตอนแก้ config ───────────
EXEC #AddMenu '/wf/health', 'GRP_WF_ENGINE', N'ตรวจสุขภาพ Workflow', 'Workflow Health Check', 7, 'WF_WORKFLOW_ADMIN';

-- ── 3. เวลาที่ใช้ — อยู่ติดกับภาพรวมงาน เพราะเป็นรายงานเหมือนกัน ─────────────
EXEC #AddMenu '/wf/cycle-time', 'GRP_WF_ENGINE', N'เวลาที่ใช้ในแต่ละขั้น', 'Cycle Time by Step', 106, 'WF_WORKFLOW_ADMIN';
GO

DROP PROCEDURE #AddMenu;
GO

PRINT '--- เมนูที่เพิ่ม และใครเห็นบ้าง ---';
SELECT m.menuid, m.uppermenucode, m.menuorder, ISNULL(m.menucode, '-') AS menucode, m.menuname,
       CAST(m.url AS nvarchar(200)) AS url,
       STUFF((SELECT ', ' + r.rolecode FROM sc_role_menu rm JOIN sc_role r ON r.roleid = rm.roleid
              WHERE rm.menuid = m.menuid ORDER BY r.rolecode FOR XML PATH('')), 1, 2, '') AS roles
FROM   sc_menu m
WHERE  CAST(m.url AS nvarchar(400)) IN ('/wf/delegations', '/wf/health', '/wf/cycle-time')
ORDER  BY m.uppermenucode, m.menuorder;
GO
