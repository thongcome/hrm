using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAnomalyDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PayrollAnomaly",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    PayrollEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    AnomalyType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetectedValue = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ReferenceValue = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    DetectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    AcknowledgedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollAnomaly", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollAnomaly_Pay_PayrollEmployee_PayrollEmployeeId",
                        column: x => x.PayrollEmployeeId,
                        principalTable: "Pay_PayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollAnomaly_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollAnomaly_PayrollEmployeeId",
                table: "Pay_PayrollAnomaly",
                column: "PayrollEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollAnomaly_PayrollRunId",
                table: "Pay_PayrollAnomaly",
                column: "PayrollRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_PayrollAnomaly");
        }
    }
}
