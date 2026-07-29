using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslipCompanyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Pay_PayslipSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyNameEn",
                table: "Pay_PayslipSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CompanyName", "CompanyNameEn" },
                values: new object[] { "Advance Digital", "Advance Digital" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "CompanyNameEn",
                table: "Pay_PayslipSettings");
        }
    }
}
