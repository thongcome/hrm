using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddProvidentFundPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ElectedByUserId",
                table: "Pay_ProvidentFundElection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "ElectedDate",
                table: "Pay_ProvidentFundElection",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "InvestmentPolicyId",
                table: "Pay_ProvidentFundElection",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundInvestmentPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RiskDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundInvestmentPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    MinEmployeeRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxEmployeeRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinCompanyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxCompanyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RateChangeLimitPerYear = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundVestingTier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    MinYearsOfService = table.Column<int>(type: "int", nullable: false),
                    MaxYearsOfService = table.Column<int>(type: "int", nullable: true),
                    VestingPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundVestingTier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundVestingTier_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundElection_InvestmentPolicyId",
                table: "Pay_ProvidentFundElection",
                column: "InvestmentPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundVestingTier_PolicyId",
                table: "Pay_ProvidentFundVestingTier",
                column: "PolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pay_ProvidentFundElection_Pay_ProvidentFundInvestmentPolicy_InvestmentPolicyId",
                table: "Pay_ProvidentFundElection",
                column: "InvestmentPolicyId",
                principalTable: "Pay_ProvidentFundInvestmentPolicy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pay_ProvidentFundElection_Pay_ProvidentFundInvestmentPolicy_InvestmentPolicyId",
                table: "Pay_ProvidentFundElection");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundInvestmentPolicy");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundVestingTier");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundPolicy");

            migrationBuilder.DropIndex(
                name: "IX_Pay_ProvidentFundElection_InvestmentPolicyId",
                table: "Pay_ProvidentFundElection");

            migrationBuilder.DropColumn(
                name: "ElectedByUserId",
                table: "Pay_ProvidentFundElection");

            migrationBuilder.DropColumn(
                name: "ElectedDate",
                table: "Pay_ProvidentFundElection");

            migrationBuilder.DropColumn(
                name: "InvestmentPolicyId",
                table: "Pay_ProvidentFundElection");
        }
    }
}
