using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class HrdRoundTwoSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OrgDev_WorkforcePlan is retired in favor of Pos_HeadcountBudget
            // (single source of truth for headcount targets — see
            // WorkforcePlanService.cs header). Carry its rows over as
            // org-scoped budget rows (PosExecTypeId NULL) before dropping,
            // skipping any (company, org, year) a budget row already covers.
            // Live DB checked 2026-08-31: 1 row in OrgDev_WorkforcePlan,
            // 0 rows in Pos_HeadcountBudget.
            migrationBuilder.Sql(@"
INSERT INTO Pos_HeadcountBudget (CompanyId, FiscalYear, OrganizationId, PosExecTypeId, ApprovedCount, Note, IsActive, CreatedDate, CreatedByUserId)
SELECT w.CompanyId, w.PlanYear, w.OrganizationId, NULL, w.TargetHeadcount, w.Note, 1, w.CreatedDate, w.CreatedByUserId
FROM OrgDev_WorkforcePlan w
WHERE NOT EXISTS (
    SELECT 1 FROM Pos_HeadcountBudget b
    WHERE b.CompanyId = w.CompanyId AND b.FiscalYear = w.PlanYear
      AND b.OrganizationId = w.OrganizationId AND b.PosExecTypeId IS NULL AND b.IsActive = 1);");

            migrationBuilder.DropTable(
                name: "OrgDev_WorkforcePlan");

            migrationBuilder.AddColumn<long>(
                name: "CompetencyId",
                table: "Lms_Course",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobMasterId",
                table: "Km_Article",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedDate",
                table: "Km_Article",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "Idp_DevelopmentAction",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetencyId",
                table: "Lms_Course");

            migrationBuilder.DropColumn(
                name: "JobMasterId",
                table: "Km_Article");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "Km_Article");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Idp_DevelopmentAction");

            migrationBuilder.CreateTable(
                name: "OrgDev_WorkforcePlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    PlanYear = table.Column<int>(type: "int", nullable: false),
                    TargetHeadcount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgDev_WorkforcePlan", x => x.Id);
                });
        }
    }
}
