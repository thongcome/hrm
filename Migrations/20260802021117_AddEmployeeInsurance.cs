using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeInsurance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceCompanyAmount",
                table: "Pay_PayrollEmployee",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceEmployeeAmount",
                table: "Pay_PayrollEmployee",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Pay_InsurancePlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CoverageAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    DefaultEmployeeAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    DefaultCompanyAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_InsurancePlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_EmployeeInsuranceEnrollment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CompanyAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_EmployeeInsuranceEnrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeInsuranceEnrollment_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeInsuranceEnrollment_Pay_InsurancePlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Pay_InsurancePlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeInsuranceEnrollment_HremployeeId",
                table: "Pay_EmployeeInsuranceEnrollment",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeInsuranceEnrollment_PlanId",
                table: "Pay_EmployeeInsuranceEnrollment",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropTable(
                name: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "InsuranceCompanyAmount",
                table: "Pay_PayrollEmployee");

            migrationBuilder.DropColumn(
                name: "InsuranceEmployeeAmount",
                table: "Pay_PayrollEmployee");
        }
    }
}
