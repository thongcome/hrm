using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddIdpModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Idp_CompetencyAssessment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    CompetencyId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    RatedLevel = table.Column<int>(type: "int", nullable: false),
                    RatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idp_CompetencyAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Idp_CompetencyAssessment_Comp_Competency_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Idp_Plan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idp_Plan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Idp_DevelopmentAction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    CompetencyId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idp_DevelopmentAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Idp_DevelopmentAction_Comp_Competency_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Idp_DevelopmentAction_Idp_Plan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Idp_Plan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Idp_CompetencyAssessment_CompetencyId",
                table: "Idp_CompetencyAssessment",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Idp_DevelopmentAction_CompetencyId",
                table: "Idp_DevelopmentAction",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Idp_DevelopmentAction_PlanId",
                table: "Idp_DevelopmentAction",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Idp_CompetencyAssessment");

            migrationBuilder.DropTable(
                name: "Idp_DevelopmentAction");

            migrationBuilder.DropTable(
                name: "Idp_Plan");
        }
    }
}
