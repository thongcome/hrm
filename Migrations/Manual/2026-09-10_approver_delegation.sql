-- ============================================================================
--  ตารางมอบฉันทะการอนุมัติ
--
--  ข้อเสนอ BA ข้อ 3 (CEO สั่งทำทุกข้อ 10 ก.ย. 2569)
--
--  "ช่วงวันที่นี้ ถ้างานจะไปถึงคนนี้ ให้ไปหาอีกคนแทน"
--  แทรกที่จุดหาผู้อนุมัติจุดเดียว (WorkflowService.GetUserRelateAsync) ทุก
--  workflow จึงได้ผลพร้อมกันโดยไม่ต้องแตะ config ของ workflow ไหนเลย
--
--  ตรวจแล้วว่าไม่มีตารางเดิมที่ใช้แทนได้ — wf_customer_approver เป็นการระบุ
--  ผู้อนุมัติตามหน่วยงาน/ตำแหน่ง ไม่ใช่การมอบตัวบุคคลชั่วคราว และทั้ง JSP กับ
--  epms ไม่มีแนวคิดนี้เลยทั้งคู่
--
--  ไม่มี FK ไป sc_user โดยตั้งใจ: ตารางนี้เป็นบันทึกว่า "เคยมอบให้ใคร" ถ้าลบ
--  ผู้ใช้ทิ้งภายหลัง ประวัติการมอบต้องไม่หายตามไปด้วย (แนวเดียวกับ job_user_list)
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

IF OBJECT_ID('Wf_ApproverDelegation', 'U') IS NULL
BEGIN
    CREATE TABLE Wf_ApproverDelegation (
        Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Wf_ApproverDelegation PRIMARY KEY,
        FromUserId      bigint        NOT NULL,   -- ผู้มอบ
        ToUserId        bigint        NOT NULL,   -- ผู้รับมอบ
        WorkflowId      bigint        NULL,       -- ว่าง = ทุกประเภทงาน
        StartDate       datetime      NOT NULL,
        EndDate         datetime      NOT NULL,   -- วันสุดท้ายที่ยังมอบอยู่ (รวมทั้งวัน)
        Reason          nvarchar(500) NULL,
        IsActive        bit           NOT NULL CONSTRAINT DF_Wf_ApproverDelegation_IsActive DEFAULT (1),
        CreatedByUserId bigint        NULL,
        CreatedAt       datetime      NOT NULL CONSTRAINT DF_Wf_ApproverDelegation_CreatedAt DEFAULT (GETDATE())
    );

    -- คำถามเดียวที่ engine ถามตอนหาผู้อนุมัติคือ "คนนี้มอบให้ใครอยู่ไหม วันนี้"
    CREATE INDEX IX_Wf_ApproverDelegation_From
        ON Wf_ApproverDelegation (FromUserId, IsActive, StartDate, EndDate);
END
GO

PRINT '--- โครงตาราง ---';
SELECT c.name, t.name AS typ, c.is_nullable
FROM   sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE  c.object_id = OBJECT_ID('Wf_ApproverDelegation')
ORDER  BY c.column_id;
GO
