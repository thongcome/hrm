using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ภาษีค่าชดเชยเลิกจ้าง (H-01): ตารางกติกามีวันเริ่มมีผล + 2 ช่องบนรายการเฉพาะกิจเก็บส่วนที่ยกเว้น/ค่าใช้จ่าย
    // ณ วันที่ส่งจ่าย (snapshot — แก้กติกาทีหลังไม่กระทบรายการที่จ่ายไปแล้ว) + เมนูใต้กลุ่มเงินเดือน
    // seed 2 แถว: อัตราเดิม (300 วัน/300,000) และอัตราตั้งแต่ปีภาษี 2566 (400 วัน/600,000) — ต้องให้ผู้รู้ภาษียืนยัน
    public partial class AddSeveranceTaxRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_SeveranceTaxRule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ExemptDays = table.Column<int>(type: "int", nullable: false),
                    ExemptCap = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ExpensePerYear = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    RemainderExpenseRate = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    RemainderExpenseCap = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EnteredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Pay_SeveranceTaxRule", x => x.Id));

            migrationBuilder.AddColumn<decimal>(name: "TaxExemptAmount", table: "Pay_AdhocPayItem", type: "decimal(15,2)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "TaxExpenseDeductionAmount", table: "Pay_AdhocPayItem", type: "decimal(15,2)", nullable: true);

            migrationBuilder.Sql(@"
INSERT INTO Pay_SeveranceTaxRule (EffectiveFrom, ExemptDays, ExemptCap, ExpensePerYear, RemainderExpenseRate, RemainderExpenseCap, IsActive, Note, EnteredByUserId, EnteredDate)
VALUES ('2000-01-01', 300, 300000, 7000, 0.5, NULL, 1, N'อัตราเดิมก่อนปีภาษี 2566 (300 วัน/300,000 บาท) — ค่าใช้จ่ายส่วนเกินใช้ชุดเดียวกับอัตราใหม่ ต้องให้ผู้รู้ภาษียืนยัน', 0, GETDATE()),
       ('2023-01-01', 400, 600000, 7000, 0.5, NULL, 1, N'ตั้งแต่ปีภาษี 2566: 400 วันสุดท้าย ไม่เกิน 600,000 บาท (มติ ครม. 18 มิ.ย. 2567, PRD); ส่วนเกินหัก 7,000×ปีที่ทำงาน แล้วหัก 50% (SCB) — ยังไม่ได้ยืนยันกับราชกิจจานุเบกษา ตรวจ 20 ก.ย. 2569', 0, GETDATE());

IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE url LIKE '/pay/admin/severance-tax')
BEGIN
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, isshow, icon, url, moddate, modby, uppermenucode, menugroupid, isactive, menuname_en)
    SELECT N'ภาษีค่าชดเชย', menulevel, isfinal, menuorder + 1, menucode, isshow, N'Icons.Material.Filled.Gavel', '/pay/admin/severance-tax', GETDATE(), 'migration', uppermenucode, menugroupid, 1, N'Severance Tax Rule'
    FROM sc_menu WHERE url LIKE '/pay/admin/minimum-wage';
END
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT nm.menuid, rm.roleid, rm.isactive, rm.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_menu nm
JOIN sc_menu om ON om.url LIKE '/pay/admin/minimum-wage'
JOIN sc_role_menu rm ON rm.menuid = om.menuid
WHERE nm.url LIKE '/pay/admin/severance-tax'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = nm.menuid AND x.roleid = rm.roleid);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_role_menu WHERE menuid IN (SELECT menuid FROM sc_menu WHERE url LIKE '/pay/admin/severance-tax'); DELETE FROM sc_menu WHERE url LIKE '/pay/admin/severance-tax';");
            migrationBuilder.DropColumn(name: "TaxExpenseDeductionAmount", table: "Pay_AdhocPayItem");
            migrationBuilder.DropColumn(name: "TaxExemptAmount", table: "Pay_AdhocPayItem");
            migrationBuilder.DropTable(name: "Pay_SeveranceTaxRule");
        }
    }
}
