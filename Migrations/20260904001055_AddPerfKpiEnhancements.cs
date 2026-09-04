using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfKpiEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "update_by",
                table: "pos_position_level",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EvaluationTypeId",
                table: "Perf_RatingScaleDescription",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectGrade",
                table: "Perf_RaterAssignment",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DirectScorePercent",
                table: "Perf_RaterAssignment",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetDistributionPercent",
                table: "Perf_GradeBand",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MethodType",
                table: "Perf_EvaluationType",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MeritAdjustmentPercent",
                table: "Perf_EvaluationInstance",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeritAdjustmentReason",
                table: "Perf_EvaluationInstance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Perf_ResultMetric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HigherIsBetter = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_ResultMetric", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_ResultMetric_Perf_EvaluationInstance_EvaluationInstanceId",
                        column: x => x.EvaluationInstanceId,
                        principalTable: "Perf_EvaluationInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perf_ResultMetric_EvaluationInstanceId",
                table: "Perf_ResultMetric",
                column: "EvaluationInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perf_ResultMetric");

            migrationBuilder.DropColumn(
                name: "EvaluationTypeId",
                table: "Perf_RatingScaleDescription");

            migrationBuilder.DropColumn(
                name: "DirectGrade",
                table: "Perf_RaterAssignment");

            migrationBuilder.DropColumn(
                name: "DirectScorePercent",
                table: "Perf_RaterAssignment");

            migrationBuilder.DropColumn(
                name: "TargetDistributionPercent",
                table: "Perf_GradeBand");

            migrationBuilder.DropColumn(
                name: "MethodType",
                table: "Perf_EvaluationType");

            migrationBuilder.DropColumn(
                name: "MeritAdjustmentPercent",
                table: "Perf_EvaluationInstance");

            migrationBuilder.DropColumn(
                name: "MeritAdjustmentReason",
                table: "Perf_EvaluationInstance");

            migrationBuilder.AlterColumn<string>(
                name: "update_by",
                table: "pos_position_level",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}
