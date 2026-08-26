using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddProvidentFundRateMatrixExitWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseFundMembershipYearsForVesting",
                table: "Pay_ProvidentFundPolicy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundExitReasonRule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    RequiresAgeAndMembershipCheck = table.Column<bool>(type: "bit", nullable: false),
                    MinAgeForException = table.Column<int>(type: "int", nullable: true),
                    MinMembershipYearsForException = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundExitReasonRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundExitReasonRule_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundMembershipPeriod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    JoinDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LeaveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundMembershipPeriod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundMembershipPeriod_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundRateChangeRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedEmployeeRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SuggestedCompanyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RequestedCompanyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WindowId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEmployeeInitiated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundRateChangeRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundRateChangeRequest_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundRateChangeRequest_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundRateChangeWindow",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenFromMonth = table.Column<int>(type: "int", nullable: false),
                    OpenFromDay = table.Column<int>(type: "int", nullable: false),
                    OpenToMonth = table.Column<int>(type: "int", nullable: false),
                    OpenToDay = table.Column<int>(type: "int", nullable: false),
                    EffectiveMonth = table.Column<int>(type: "int", nullable: false),
                    EffectiveDay = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundRateChangeWindow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundRateChangeWindow_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundRateMatrixRule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    MinYearsOfService = table.Column<int>(type: "int", nullable: false),
                    MaxYearsOfService = table.Column<int>(type: "int", nullable: true),
                    EmployeeRateMin = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EmployeeRateMax = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    FixedCompanyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundRateMatrixRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundRateMatrixRule_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundExitCase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    ExitReasonRuleId = table.Column<long>(type: "bigint", nullable: false),
                    ExitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ComputedVestingPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EmployeeContributionAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CompanyAmountToEmployee = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CompanyAmountReturnedToEmployer = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundExitCase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundExitCase_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundExitCase_Pay_ProvidentFundExitReasonRule_ExitReasonRuleId",
                        column: x => x.ExitReasonRuleId,
                        principalTable: "Pay_ProvidentFundExitReasonRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundExitCase_Pay_ProvidentFundPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Pay_ProvidentFundPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundCalculationDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    RateChangeRequestId = table.Column<long>(type: "bigint", nullable: true),
                    ExitCaseId = table.Column<long>(type: "bigint", nullable: true),
                    InputsSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MatchedRuleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultValue = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CalculatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundCalculationDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundCalculationDetail_Pay_ProvidentFundExitCase_ExitCaseId",
                        column: x => x.ExitCaseId,
                        principalTable: "Pay_ProvidentFundExitCase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundCalculationDetail_Pay_ProvidentFundRateChangeRequest_RateChangeRequestId",
                        column: x => x.RateChangeRequestId,
                        principalTable: "Pay_ProvidentFundRateChangeRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundCalculationDetail_ExitCaseId",
                table: "Pay_ProvidentFundCalculationDetail",
                column: "ExitCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundCalculationDetail_RateChangeRequestId",
                table: "Pay_ProvidentFundCalculationDetail",
                column: "RateChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundExitCase_ExitReasonRuleId",
                table: "Pay_ProvidentFundExitCase",
                column: "ExitReasonRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundExitCase_HremployeeId",
                table: "Pay_ProvidentFundExitCase",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundExitCase_PolicyId",
                table: "Pay_ProvidentFundExitCase",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundExitReasonRule_PolicyId",
                table: "Pay_ProvidentFundExitReasonRule",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundMembershipPeriod_HremployeeId",
                table: "Pay_ProvidentFundMembershipPeriod",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundRateChangeRequest_HremployeeId",
                table: "Pay_ProvidentFundRateChangeRequest",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundRateChangeRequest_PolicyId",
                table: "Pay_ProvidentFundRateChangeRequest",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundRateChangeWindow_PolicyId",
                table: "Pay_ProvidentFundRateChangeWindow",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundRateMatrixRule_PolicyId",
                table: "Pay_ProvidentFundRateMatrixRule",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundCalculationDetail");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundMembershipPeriod");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundRateChangeWindow");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundRateMatrixRule");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundExitCase");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundRateChangeRequest");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundExitReasonRule");

            migrationBuilder.DropColumn(
                name: "UseFundMembershipYearsForVesting",
                table: "Pay_ProvidentFundPolicy");
        }
    }
}
