using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedSeverancePayItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 11, 0, "SEVERANCE", 1, "5040-SEVERANCE", true, true, "Statutory Severance Pay", "ค่าชดเชยตามกฎหมาย", 11 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
