using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslipSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PayslipSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PasswordTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayslipSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Pay_PayslipSettings",
                columns: new[] { "Id", "CompanyId", "ModifiedByUserId", "ModifiedDate", "PasswordTemplate" },
                values: new object[] { 1L, "001", null, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "{BirthDateDDMMYYYY}" });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayslipSettings_CompanyId",
                table: "Pay_PayslipSettings",
                column: "CompanyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_PayslipSettings");
        }
    }
}
