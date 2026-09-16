using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // com_organization node ที่ isCompany=true แต่ไม่มี companyid เชื่อมไป
    // com_company (เช่น ADHOLD/ADDIGITAL/ADMOVIE — องค์กรลูกที่ยังไม่ได้สร้างแถว
    // com_company ของตัวเอง) ไม่มีที่เก็บข้อมูล ประเภทนิติบุคคล/VAT/พันธกิจ/สโลแกน/
    // แผนที่ เลย — mirror ชุดคอลัมน์เดียวกับ com_company มาไว้ที่นี่ (CEO, 16 ก.ย. 2569).
    public partial class AddComOrganizationCompanyInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessTypeName",
                table: "com_organization",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatRegistered",
                table: "com_organization",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mission",
                table: "com_organization",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slogan",
                table: "com_organization",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapUrl",
                table: "com_organization",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BusinessTypeName", table: "com_organization");
            migrationBuilder.DropColumn(name: "VatRegistered", table: "com_organization");
            migrationBuilder.DropColumn(name: "mission", table: "com_organization");
            migrationBuilder.DropColumn(name: "slogan", table: "com_organization");
            migrationBuilder.DropColumn(name: "MapUrl", table: "com_organization");
        }
    }
}
