using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // เพดานชั่วโมง OT ต่อสัปดาห์ (ตารางตั้งค่ามีวันเริ่มมีผล ไม่ seed ตัวเลข) + เมนู "เพดาน OT" ใต้กลุ่มเวลาทำงาน
    // (สิทธิ์เมนูคัดลอกจากหน้าตัวคูณ OT /att/ot-rules)
    public partial class AddOtPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Att_OtPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    WeeklyCapHours = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    BlockOnExceed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EnteredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Att_OtPolicy", x => x.Id));

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE url LIKE '/att/ot-policy')
BEGIN
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, isshow, icon, url, moddate, modby, uppermenucode, menugroupid, isactive, menuname_en)
    SELECT N'เพดาน OT ต่อสัปดาห์', menulevel, isfinal, menuorder + 1, menucode, isshow, N'Icons.Material.Filled.Timer', '/att/ot-policy', GETDATE(), 'migration', uppermenucode, menugroupid, 1, N'Weekly OT Cap'
    FROM sc_menu WHERE url LIKE '/att/ot-rules';
END
INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT nm.menuid, rm.roleid, rm.isactive, rm.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_menu nm
JOIN sc_menu om ON om.url LIKE '/att/ot-rules'
JOIN sc_role_menu rm ON rm.menuid = om.menuid
WHERE nm.url LIKE '/att/ot-policy'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = nm.menuid AND x.roleid = rm.roleid);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_role_menu WHERE menuid IN (SELECT menuid FROM sc_menu WHERE url LIKE '/att/ot-policy'); DELETE FROM sc_menu WHERE url LIKE '/att/ot-policy';");
            migrationBuilder.DropTable(name: "Att_OtPolicy");
        }
    }
}
