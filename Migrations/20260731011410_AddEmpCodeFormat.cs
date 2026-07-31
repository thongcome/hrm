using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpCodeFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpCodeDigits",
                table: "Pay_PayslipSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmpCodePrefix",
                table: "Pay_PayslipSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "EmpCodeDigits", "EmpCodePrefix" },
                values: new object[] { 3, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpCodeDigits",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "EmpCodePrefix",
                table: "Pay_PayslipSettings");
        }
    }
}
