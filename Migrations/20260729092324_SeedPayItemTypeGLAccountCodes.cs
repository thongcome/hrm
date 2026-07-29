using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedPayItemTypeGLAccountCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 1,
                column: "GLAccountCode",
                value: "5000-SALARY");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 2,
                column: "GLAccountCode",
                value: "5010-OT");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 3,
                column: "GLAccountCode",
                value: "5020-ALLOWANCE");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 4,
                column: "GLAccountCode",
                value: "2200-SSO-PAYABLE");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 5,
                column: "GLAccountCode",
                value: "2210-PF-PAYABLE");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 6,
                column: "GLAccountCode",
                value: "2220-WHT-PAYABLE");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 7,
                column: "GLAccountCode",
                value: "1300-LOAN-RECEIVABLE");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 8,
                column: "GLAccountCode",
                value: "5090-ADJUSTMENT");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 9,
                column: "GLAccountCode",
                value: "5030-BONUS");

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 10,
                column: "GLAccountCode",
                value: "2230-ADHOC-PAYABLE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 1,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 2,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 3,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 4,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 5,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 6,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 7,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 8,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 9,
                column: "GLAccountCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 10,
                column: "GLAccountCode",
                value: null);
        }
    }
}
