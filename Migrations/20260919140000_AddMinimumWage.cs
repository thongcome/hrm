using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ค่าจ้างขั้นต่ำเป็นตารางตั้งค่า (จังหวัด + อัตรา/วัน + วันเริ่มมีผล) + จังหวัดที่ตั้งสถานประกอบการในตั้งค่าสลิป
    // + เมนู "ค่าจ้างขั้นต่ำ" ใต้กลุ่มเงินเดือน (สิทธิ์เมนูคัดลอกจากหน้ารูปแบบไฟล์ธนาคาร) — ไม่ seed อัตราจริง ต้องกรอกจากประกาศ
    public partial class AddMinimumWage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_MinimumWage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ProvinceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DailyAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EnteredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Pay_MinimumWage", x => x.Id));

            migrationBuilder.AddColumn<string>(name: "WorkProvince", table: "Pay_PayslipSettings", type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE url LIKE '/pay/admin/minimum-wage')
BEGIN
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, isshow, icon, url, moddate, modby, uppermenucode, menugroupid, isactive, menuname_en)
    SELECT N'ค่าจ้างขั้นต่ำ', menulevel, isfinal, menuorder + 1, menucode, isshow, N'Icons.Material.Filled.Paid', '/pay/admin/minimum-wage', GETDATE(), 'migration', uppermenucode, menugroupid, 1, N'Minimum Wage'
    FROM sc_menu WHERE url LIKE '/pay/admin/bank-file-formats';
END
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT nm.menuid, rm.roleid, rm.isactive, rm.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_menu nm
JOIN sc_menu om ON om.url LIKE '/pay/admin/bank-file-formats'
JOIN sc_role_menu rm ON rm.menuid = om.menuid
WHERE nm.url LIKE '/pay/admin/minimum-wage'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = nm.menuid AND x.roleid = rm.roleid);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_role_menu WHERE menuid IN (SELECT menuid FROM sc_menu WHERE url LIKE '/pay/admin/minimum-wage'); DELETE FROM sc_menu WHERE url LIKE '/pay/admin/minimum-wage';");
            migrationBuilder.DropColumn(name: "WorkProvince", table: "Pay_PayslipSettings");
            migrationBuilder.DropTable(name: "Pay_MinimumWage");
        }
    }
}
