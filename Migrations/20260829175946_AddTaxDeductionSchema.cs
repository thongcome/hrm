using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxDeductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxDeductionAmount",
                table: "Pay_PayrollEmployee",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Pay_TaxDeductionSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EffectiveYear = table.Column<int>(type: "int", nullable: false),
                    PersonalAllowancePerYear = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ExpenseDeductionRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    ExpenseDeductionCap = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_TaxDeductionSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_TaxDeductionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EffectiveYear = table.Column<int>(type: "int", nullable: false),
                    NameTh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxAmountPerYear = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_TaxDeductionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_EmployeeTaxDeductionElection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    DeductionTypeId = table.Column<int>(type: "int", nullable: false),
                    AnnualAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ApplyMonthly = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ElectedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ElectedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_EmployeeTaxDeductionElection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeTaxDeductionElection_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeTaxDeductionElection_Pay_TaxDeductionType_DeductionTypeId",
                        column: x => x.DeductionTypeId,
                        principalTable: "Pay_TaxDeductionType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeTaxDeductionElection_DeductionTypeId",
                table: "Pay_EmployeeTaxDeductionElection",
                column: "DeductionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeTaxDeductionElection_HremployeeId",
                table: "Pay_EmployeeTaxDeductionElection",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_EmployeeTaxDeductionElection");

            migrationBuilder.DropTable(
                name: "Pay_TaxDeductionSetting");

            migrationBuilder.DropTable(
                name: "Pay_TaxDeductionType");

            migrationBuilder.DropColumn(
                name: "TaxDeductionAmount",
                table: "Pay_PayrollEmployee");
        }
    }
}
