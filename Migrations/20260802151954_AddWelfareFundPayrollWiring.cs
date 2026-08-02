using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareFundPayrollWiring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WelfareFundCompanyAmount",
                table: "Pay_PayrollEmployee",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WelfareFundEmployeeAmount",
                table: "Pay_PayrollEmployee",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 13, 1, "WELFAREFUND", -1, "2260-WF-PAYABLE", true, true, "Employee Welfare Fund Deduction", "หักกองทุนสงเคราะห์ลูกจ้าง", 13 }
                });

            // Refresh Note text on the two seeded policy rows (Id 1, 2) now
            // that the statutory citations (พ.ร.บ.คุ้มครองแรงงาน มาตรา
            // 130/131/133) have been confirmed directly against the primary
            // legal text — see Pay_WelfareFundPolicy.cs for the full
            // reasoning. IsEnabled is left untouched (still false).
            migrationBuilder.UpdateData(
                table: "Pay_WelfareFundPolicy",
                keyColumn: "Id",
                keyValue: 1L,
                column: "Note",
                value: "ยืนยันจากตัวบทกฎหมาย (พ.ร.บ.คุ้มครองแรงงาน ม.130-131): บังคับเฉพาะนายจ้าง 10 คนขึ้นไป เว้นแต่มี PF/สวัสดิการอื่นครอบคลุมอยู่แล้ว, อัตราตามกฎหมายต้องไม่เกิน 5%, ไม่มีเพดานค่าจ้างในตัวบทกฎหมาย — อัตรา 0.25%/0.25% และวันที่เริ่ม 1 ต.ค. 2569 ยืนยันจากเว็บกรมสวัสดิการฯ (ewf.labour.go.th) + หลายแหล่งข่าว — วันสิ้นสุดช่วงนี้ (30 ก.ย. 2574) ยังมีแหล่งข้อมูลบางส่วนระบุ 2573 ต่างกัน ควรตรวจสอบก่อนเปิดใช้งานจริง");

            migrationBuilder.UpdateData(
                table: "Pay_WelfareFundPolicy",
                keyColumn: "Id",
                keyValue: 2L,
                column: "Note",
                value: "อัตราขั้นที่ 2 (0.5%/0.5%) ตามกฎกระทรวงที่เผยแพร่ — เงื่อนไขทางกฎหมายเดียวกับแถวแรก (ม.130-131) วันเริ่มของช่วงนี้ผูกกับวันสิ้นสุดช่วงแรกที่ยังมีแหล่งข้อมูลขัดแย้งกันเล็กน้อย (2573 หรือ 2574) ควรตรวจสอบก่อนเปิดใช้งานจริง");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "WelfareFundCompanyAmount",
                table: "Pay_PayrollEmployee");

            migrationBuilder.DropColumn(
                name: "WelfareFundEmployeeAmount",
                table: "Pay_PayrollEmployee");
        }
    }
}
