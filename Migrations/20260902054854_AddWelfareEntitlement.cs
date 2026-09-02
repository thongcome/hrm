using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareEntitlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wel_Entitlements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    BenefitTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    OverrideAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    OverrideMaxClaimsPerYear = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SetByUserId = table.Column<long>(type: "bigint", nullable: true),
                    SetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wel_Entitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wel_Entitlements_Wel_BenefitTypes_BenefitTypeId",
                        column: x => x.BenefitTypeId,
                        principalTable: "Wel_BenefitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wel_Entitlements_BenefitTypeId",
                table: "Wel_Entitlements",
                column: "BenefitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Wel_Entitlements_CompanyId_BenefitTypeId_IsActive",
                table: "Wel_Entitlements",
                columns: new[] { "CompanyId", "BenefitTypeId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wel_Entitlements");
        }
    }
}
