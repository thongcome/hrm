using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ยอดยกมา (opening balance) ของบริษัทนี้เองเมื่อเริ่มใช้ระบบกลางปี — ใช้ตารางเดิม Pay_EmployeePriorEmployerIncome
    // แทนการสร้างตารางใหม่ (ตัวเลขชุดเดียวกัน: เงินได้/ลดหย่อน/ภาษี) เพิ่มธง IsSameEmployer + ประกันสังคม/กองทุนสำหรับ 50 ทวิ
    public partial class AddOpeningBalanceToPriorIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "IsSameEmployer", table: "Pay_EmployeePriorEmployerIncome", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<decimal>(name: "SocialSecurityAmount", table: "Pay_EmployeePriorEmployerIncome", type: "decimal(15,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "ProvidentFundAmount", table: "Pay_EmployeePriorEmployerIncome", type: "decimal(15,2)", nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsSameEmployer", table: "Pay_EmployeePriorEmployerIncome");
            migrationBuilder.DropColumn(name: "SocialSecurityAmount", table: "Pay_EmployeePriorEmployerIncome");
            migrationBuilder.DropColumn(name: "ProvidentFundAmount", table: "Pay_EmployeePriorEmployerIncome");
        }
    }
}
