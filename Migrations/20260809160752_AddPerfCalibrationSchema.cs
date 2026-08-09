using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfCalibrationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perf_CalibrationSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_CalibrationSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_CalibrationSession_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_CalibrationAdjustment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    InstanceId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalScorePercent = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    OriginalGrade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AdjustedScorePercent = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    AdjustedGrade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdjustedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdjustedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_CalibrationAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_CalibrationAdjustment_Perf_CalibrationSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Perf_CalibrationSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perf_CalibrationAdjustment_SessionId",
                table: "Perf_CalibrationAdjustment",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_CalibrationSession_EvaluationPeriodId",
                table: "Perf_CalibrationSession",
                column: "EvaluationPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perf_CalibrationAdjustment");

            migrationBuilder.DropTable(
                name: "Perf_CalibrationSession");
        }
    }
}
