using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslipCompanyTaxInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "Pay_PayslipSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyTaxId",
                table: "Pay_PayslipSettings",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CompanyAddress", "CompanyTaxId" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "CompanyTaxId",
                table: "Pay_PayslipSettings");
        }
    }
}
