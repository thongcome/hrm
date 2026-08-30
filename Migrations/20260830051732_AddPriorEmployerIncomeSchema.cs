using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorEmployerIncomeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_EmployeePriorEmployerIncome",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    PriorEmployerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IncomeAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TaxWithheldAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EnteredDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_EmployeePriorEmployerIncome", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_EmployeePriorEmployerIncome_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_EmployeePriorEmployerIncome_HremployeeId",
                table: "Pay_EmployeePriorEmployerIncome",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_EmployeePriorEmployerIncome");
        }
    }
}
