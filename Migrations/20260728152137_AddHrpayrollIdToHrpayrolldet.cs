using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddHrpayrollIdToHrpayrolldet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HrpayrollId",
                table: "HRPAYROLLDET",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HRPAYROLLDET_HrpayrollId",
                table: "HRPAYROLLDET",
                column: "HrpayrollId");

            migrationBuilder.AddForeignKey(
                name: "FK_HRPAYROLLDET_HRPAYROLL_HrpayrollId",
                table: "HRPAYROLLDET",
                column: "HrpayrollId",
                principalTable: "HRPAYROLL",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HRPAYROLLDET_HRPAYROLL_HrpayrollId",
                table: "HRPAYROLLDET");

            migrationBuilder.DropIndex(
                name: "IX_HRPAYROLLDET_HrpayrollId",
                table: "HRPAYROLLDET");

            migrationBuilder.DropColumn(
                name: "HrpayrollId",
                table: "HRPAYROLLDET");
        }
    }
}
