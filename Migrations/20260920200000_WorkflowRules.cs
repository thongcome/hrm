using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // พอร์ตกฎ workflow ข้อ 4, 13, 14 จาก Advance.Payroll (handoff #7)
    //  · ข้อ 13/14: wf_sub_workflow_master.isNeedsupervisorapprove เปลี่ยนจาก int? (จำนวนชั้น) เป็น bit NOT NULL
    //    จำนวนชั้นย้ายไปอยู่ verticalMaxLevel — ย้ายข้อมูลก่อนแปลงชนิด (แถวที่ตั้ง 3 ชั้นต้องไม่กลายเป็น 1 ชั้น)
    //  · ข้อ 4: ปุ่ม "อนุมัติและส่งต่อ" (approvenext) สำหรับขั้นกลาง แยกจาก "ส่งต่อ" (submit) ของผู้ยื่นที่ขั้นร่าง
    //    "อนุมัติ" ของขั้นสุดท้ายเปลี่ยนชื่อเป็น "อนุมัติ (สิ้นสุด)" ให้ผู้ใช้เห็นต่างกันชัดเจน
    // Raw SQL ทั้งหมด จึงใช้ Designer แบบสั้นได้ (ดู CLAUDE.md หัวข้อ Build time)
    public partial class WorkflowRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) ย้ายจำนวนชั้นไป verticalMaxLevel ก่อน แล้วค่อยแปลงเป็น bit
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('wf_sub_workflow_master') AND c.name = 'isNeedsupervisorapprove' AND t.name = 'int')
BEGIN
    EXEC('UPDATE wf_sub_workflow_master SET verticalMaxLevel = isNeedsupervisorapprove WHERE isNeedsupervisorapprove > 0');
    EXEC('UPDATE wf_sub_workflow_master SET isNeedsupervisorapprove = CASE WHEN isNeedsupervisorapprove > 0 THEN 1 ELSE 0 END');
    EXEC('ALTER TABLE wf_sub_workflow_master ALTER COLUMN isNeedsupervisorapprove bit NOT NULL');
    EXEC('ALTER TABLE wf_sub_workflow_master ADD CONSTRAINT DF_wf_sub_workflow_master_isNeedsupervisorapprove DEFAULT 0 FOR isNeedsupervisorapprove');
END");

            // 2) ปุ่ม approvenext + เปลี่ยนชื่อ approve + ย้ายแถวขั้นกลางจาก submit ไป approvenext
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM wf_button_master WHERE code = 'approvenext')
    INSERT INTO wf_button_master (name, code, class_style, moddate, modby, actiontypecode, value, btnType, orderth)
    VALUES (N'อนุมัติและส่งต่อ', 'approvenext', 'btn btn-primary', GETDATE(), 'migration', 'approvenext', N'อนุมัติและส่งต่อ', 'submit', 1);

UPDATE wf_button_master SET name = N'อนุมัติ (สิ้นสุด)', value = N'อนุมัติ (สิ้นสุด)'
 WHERE code = 'approve' AND value = N'อนุมัติ';

DECLARE @next bigint = (SELECT TOP 1 id FROM wf_button_master WHERE code = 'approvenext');
DECLARE @submit bigint = (SELECT TOP 1 id FROM wf_button_master WHERE code = 'submit');
IF @next IS NOT NULL AND @submit IS NOT NULL
    UPDATE wf_button SET button_masterid = @next, btname = N'อนุมัติและส่งต่อ', bcode = 'approvenext'
     WHERE button_masterid = @submit AND ISNULL(isStart, 0) = 0 AND ISNULL(istop, 0) = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @next bigint = (SELECT TOP 1 id FROM wf_button_master WHERE code = 'approvenext');
DECLARE @submit bigint = (SELECT TOP 1 id FROM wf_button_master WHERE code = 'submit');
IF @next IS NOT NULL AND @submit IS NOT NULL
    UPDATE wf_button SET button_masterid = @submit, btname = N'ส่งต่อ', bcode = 'submit' WHERE button_masterid = @next;
DELETE FROM wf_button_master WHERE code = 'approvenext';
UPDATE wf_button_master SET name = N'อนุมัติ', value = N'อนุมัติ' WHERE code = 'approve' AND value = N'อนุมัติ (สิ้นสุด)';

IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_wf_sub_workflow_master_isNeedsupervisorapprove')
    ALTER TABLE wf_sub_workflow_master DROP CONSTRAINT DF_wf_sub_workflow_master_isNeedsupervisorapprove;
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('wf_sub_workflow_master') AND c.name = 'isNeedsupervisorapprove' AND t.name = 'bit')
BEGIN
    EXEC('ALTER TABLE wf_sub_workflow_master ALTER COLUMN isNeedsupervisorapprove int NULL');
    EXEC('UPDATE wf_sub_workflow_master SET isNeedsupervisorapprove = verticalMaxLevel WHERE isNeedsupervisorapprove = 1 AND verticalMaxLevel > 0');
END");
        }
    }
}
