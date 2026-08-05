using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseClaimsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exp_ClaimHeader",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    AdhocPayItemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exp_ClaimHeader", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exp_ClaimHeader_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exp_ExpenseCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exp_ExpenseCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exp_ClaimLineItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimHeaderId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ReceiptDocCenterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exp_ClaimLineItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exp_ClaimLineItem_Exp_ClaimHeader_ClaimHeaderId",
                        column: x => x.ClaimHeaderId,
                        principalTable: "Exp_ClaimHeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exp_ClaimLineItem_Exp_ExpenseCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Exp_ExpenseCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exp_ClaimHeader_HremployeeId",
                table: "Exp_ClaimHeader",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Exp_ClaimLineItem_CategoryId",
                table: "Exp_ClaimLineItem",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Exp_ClaimLineItem_ClaimHeaderId",
                table: "Exp_ClaimLineItem",
                column: "ClaimHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exp_ClaimLineItem");

            migrationBuilder.DropTable(
                name: "Exp_ClaimHeader");

            migrationBuilder.DropTable(
                name: "Exp_ExpenseCategory");
        }
    }
}
