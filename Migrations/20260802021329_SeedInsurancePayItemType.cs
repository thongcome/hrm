using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedInsurancePayItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 12, 1, "INSURANCE", -1, "2240-INS-PAYABLE", true, true, "Group Insurance Deduction", "หักเบี้ยประกันกลุ่ม", 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 12);
        }
    }
}
