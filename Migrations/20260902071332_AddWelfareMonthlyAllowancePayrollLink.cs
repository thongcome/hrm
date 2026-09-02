using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareMonthlyAllowancePayrollLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "Wel_BenefitTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAllowanceAmount",
                table: "Wel_BenefitTypes",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayItemTypeId",
                table: "Wel_BenefitTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wel_BenefitTypes_PayItemTypeId",
                table: "Wel_BenefitTypes",
                column: "PayItemTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wel_BenefitTypes_Pay_PayItemType_PayItemTypeId",
                table: "Wel_BenefitTypes",
                column: "PayItemTypeId",
                principalTable: "Pay_PayItemType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wel_BenefitTypes_Pay_PayItemType_PayItemTypeId",
                table: "Wel_BenefitTypes");

            migrationBuilder.DropIndex(
                name: "IX_Wel_BenefitTypes_PayItemTypeId",
                table: "Wel_BenefitTypes");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "Wel_BenefitTypes");

            migrationBuilder.DropColumn(
                name: "MonthlyAllowanceAmount",
                table: "Wel_BenefitTypes");

            migrationBuilder.DropColumn(
                name: "PayItemTypeId",
                table: "Wel_BenefitTypes");
        }
    }
}
