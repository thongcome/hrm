using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfImprovementPlanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "JobMasterId",
                table: "Succ_SuccessorNominations",
                type: "bigint",
                nullable: true);

            // Existing nominations predate this workflow gate entirely — grandfather
            // them in as already-Approved (2) rather than the EF-generated default
            // of 0, which isn't even a valid SuccessionNominationStatus value (the
            // enum starts at 1) and would silently drop every pre-existing
            // nomination out of GetBenchStrengthAsync's Approved-only filter.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Succ_SuccessorNominations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "Perf_ImprovementPlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    SourceEvaluationInstanceId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ManagerUserId = table.Column<long>(type: "bigint", nullable: true),
                    PreviousPlanId = table.Column<long>(type: "bigint", nullable: true),
                    OutcomeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutcomeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_ImprovementPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perf_ImprovementCheckIn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_ImprovementCheckIn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_ImprovementCheckIn_Perf_ImprovementPlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Perf_ImprovementPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_ImprovementGoal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SuccessCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_ImprovementGoal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_ImprovementGoal_Perf_ImprovementPlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Perf_ImprovementPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perf_ImprovementCheckIn_PlanId",
                table: "Perf_ImprovementCheckIn",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_ImprovementGoal_PlanId",
                table: "Perf_ImprovementGoal",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_ImprovementPlan_HremployeeId",
                table: "Perf_ImprovementPlan",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perf_ImprovementCheckIn");

            migrationBuilder.DropTable(
                name: "Perf_ImprovementGoal");

            migrationBuilder.DropTable(
                name: "Perf_ImprovementPlan");

            migrationBuilder.DropColumn(
                name: "JobMasterId",
                table: "Succ_SuccessorNominations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Succ_SuccessorNominations");
        }
    }
}
