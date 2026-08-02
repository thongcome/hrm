using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_EmployeeLoan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TotalInstallments = table.Column<int>(type: "int", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    StartPeriod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_EmployeeLoan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeLoan_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_EmployeeLoanInstallment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<long>(type: "bigint", nullable: false),
                    InstallmentNo = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConsumedByPayrollRunId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_EmployeeLoanInstallment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeLoanInstallment_Pay_EmployeeLoan_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Pay_EmployeeLoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeeLoanInstallment_Pay_PayrollRun_ConsumedByPayrollRunId",
                        column: x => x.ConsumedByPayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeLoan_HremployeeId",
                table: "Pay_EmployeeLoan",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeLoanInstallment_ConsumedByPayrollRunId",
                table: "Pay_EmployeeLoanInstallment",
                column: "ConsumedByPayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeeLoanInstallment_LoanId",
                table: "Pay_EmployeeLoanInstallment",
                column: "LoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_EmployeeLoanInstallment");

            migrationBuilder.DropTable(
                name: "Pay_EmployeeLoan");
        }
    }
}
