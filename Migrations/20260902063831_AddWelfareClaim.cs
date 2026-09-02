using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wel_Claim",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    BenefitTypeId = table.Column<long>(type: "bigint", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptDocCenterId = table.Column<long>(type: "bigint", nullable: true),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    AdhocPayItemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wel_Claim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wel_Claim_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wel_Claim_Wel_BenefitTypes_BenefitTypeId",
                        column: x => x.BenefitTypeId,
                        principalTable: "Wel_BenefitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wel_Claim_BenefitTypeId",
                table: "Wel_Claim",
                column: "BenefitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Wel_Claim_CompanyId_HremployeeId_BenefitTypeId",
                table: "Wel_Claim",
                columns: new[] { "CompanyId", "HremployeeId", "BenefitTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Wel_Claim_HremployeeId",
                table: "Wel_Claim",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wel_Claim");
        }
    }
}
