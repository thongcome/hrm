using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareFundPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_WelfareFundPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeContributionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CompanyContributionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WageCapPerMonth = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_WelfareFundPolicy", x => x.Id);
                });

            // Seeded from cross-referenced secondary sources (as of 2026-08),
            // NOT the primary ministerial regulation text — rate and
            // effective-date schedule are corroborated by multiple sources,
            // but the wage cap could not be confirmed (left null = uncapped).
            // IsEnabled=false so this has zero effect on real payroll until
            // an HR admin reviews and turns it on via WelfareFundPolicyAdmin.
            migrationBuilder.InsertData(
                table: "Pay_WelfareFundPolicy",
                columns: new[] { "Id", "CompanyId", "EffectiveFrom", "EffectiveTo", "EmployeeContributionRate", "CompanyContributionRate", "WageCapPerMonth", "IsEnabled", "Note" },
                values: new object[,]
                {
                    { 1, "001", new DateOnly(2026, 10, 1), new DateOnly(2031, 9, 30), 0.25m, 0.25m, null, false,
                      "ค่าเริ่มต้นจากการสืบค้นข้อมูลสาธารณะ 2569-08 (ไม่ได้อ้างอิงจากกฎกระทรวง/ประกาศราชกิจจานุเบกษาโดยตรง) — ยังไม่ยืนยันเพดานค่าจ้างและช่องทางนำส่งกับกรมสวัสดิการฯ ตรวจสอบให้แน่ใจก่อนเปิดใช้งาน" },
                    { 2, "001", new DateOnly(2031, 10, 1), null, 0.5m, 0.5m, null, false,
                      "อัตราขั้นที่ 2 ตามตารางที่เผยแพร่ (1 ต.ค. 2574 เป็นต้นไป) — เงื่อนไขเดียวกับแถวแรก ยังไม่ยืนยันกับแหล่งกฎหมายหลัก" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_WelfareFundPolicy");
        }
    }
}
