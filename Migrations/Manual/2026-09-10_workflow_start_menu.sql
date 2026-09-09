-- ============================================================================
--  เมนู "เริ่มงานใหม่ (Workflow)" -> /workflow
-- ============================================================================
--  หน้า WorkflowDetail.razor มี 3 route บน component เดียว:
--    /workflow                     หน้าเริ่มงาน (เลือก workflow) + งานของฉันที่ยังไม่ปิด
--    /workflow/create/{workflowid} สร้างงานแล้วเด้งเข้า detail (epms: Create -> Detail)
--    /workflow/detail/{id}         หน้า template ของงาน
--
--  เมนูของระบบนี้อ่านจาก sc_menu + sc_role_menu ไม่ใช่ hardcode ในหน้า layout
--  จึงต้องมีแถวเมนู ไม่งั้นเข้าได้เฉพาะพิมพ์ URL เอง
--
--  รันซ้ำได้ (add-missing by url)
-- ============================================================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/workflow')
BEGIN
    -- menugroupid เป็น NOT NULL — ยืมค่าจากเมนูพี่น้องในกลุ่มเดียวกัน
    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_WF_ENGINE');
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);

    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder,
                         uppermenucode, isshow, url, isactive, menugroupid, moddate)
    VALUES (@id, N'เริ่มงานใหม่ (Workflow)', 'Start Workflow', 2, 1, 5,
            'GRP_WF_ENGINE', 1, '/workflow', 1, @grp, GETDATE());
    SET IDENTITY_INSERT sc_menu OFF;

    -- ให้สิทธิ์กับทุก role ที่เห็นเมนู Workflow Engine อื่นอยู่แล้ว
    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT rm.roleid, @id, 1, GETDATE()
    FROM sc_role_menu rm
    WHERE rm.menuid IN (SELECT menuid FROM sc_menu WHERE uppermenucode = 'GRP_WF_ENGINE')
      AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = rm.roleid AND x.menuid = @id);
END

-- ── หน้าออกแบบ Workflow (Master/Detail) ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/wf/design')
BEGIN
    DECLARE @grp2 bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_WF_ENGINE');
    DECLARE @id2  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);

    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder,
                         uppermenucode, isshow, url, isactive, menugroupid, moddate)
    VALUES (@id2, N'ออกแบบ Workflow', 'Workflow Designer', 2, 1, 6,
            'GRP_WF_ENGINE', 1, '/wf/design', 1, @grp2, GETDATE());
    SET IDENTITY_INSERT sc_menu OFF;

    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT rm.roleid, @id2, 1, GETDATE()
    FROM sc_role_menu rm
    WHERE rm.menuid IN (SELECT menuid FROM sc_menu WHERE uppermenucode = 'GRP_WF_ENGINE')
      AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = rm.roleid AND x.menuid = @id2);
END

SELECT menuid, menuname, url FROM sc_menu WHERE CAST(url AS nvarchar(400)) IN ('/workflow', '/wf/design');
