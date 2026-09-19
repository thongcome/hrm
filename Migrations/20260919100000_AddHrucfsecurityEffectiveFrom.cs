using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // audit H-03: อัตรา/เพดานประกันสังคมต้องมีวันที่มีผล (เพดานปรับเป็นขั้นตามปี) — NULL = ใช้ได้ตลอด
    // แถวเดิมทุกแถวจึงทำงานเหมือนเดิม (additive, nullable)
    public partial class AddHrucfsecurityEffectiveFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                table: "HRUCFSECURITY",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EffectiveFrom", table: "HRUCFSECURITY");
        }
    }
}
