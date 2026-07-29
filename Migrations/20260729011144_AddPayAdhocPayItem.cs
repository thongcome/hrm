using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayAdhocPayItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_AdhocPayItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PayItemTypeId = table.Column<int>(type: "int", nullable: false),
                    TargetPeriod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedByPayrollRunId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_AdhocPayItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_AdhocPayItem_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_AdhocPayItem_Pay_PayItemType_PayItemTypeId",
                        column: x => x.PayItemTypeId,
                        principalTable: "Pay_PayItemType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_AdhocPayItem_Pay_PayrollRun_ConsumedByPayrollRunId",
                        column: x => x.ConsumedByPayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 9, 0, "BONUS", 1, null, true, false, "Bonus / Commission (ad-hoc)", "โบนัส/ค่าคอมมิชชั่นเฉพาะกิจ", 9 },
                    { 10, 1, "ADHOC_DEDUCT", -1, null, true, false, "Ad-hoc Deduction", "หักเฉพาะกิจ (เช่น ค่าเสียหาย/ชุดยูนิฟอร์ม)", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_AdhocPayItem_ConsumedByPayrollRunId",
                table: "Pay_AdhocPayItem",
                column: "ConsumedByPayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_AdhocPayItem_HremployeeId",
                table: "Pay_AdhocPayItem",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_AdhocPayItem_PayItemTypeId",
                table: "Pay_AdhocPayItem",
                column: "PayItemTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_AdhocPayItem");

            migrationBuilder.DeleteData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Pay_PayItemType",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
