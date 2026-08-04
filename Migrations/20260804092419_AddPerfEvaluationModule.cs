using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfEvaluationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perf_EvaluationPeriod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScoreDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_EvaluationPeriod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perf_EvaluationType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_EvaluationType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perf_RaterDirectionConfig",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AllowSelf = table.Column<bool>(type: "bit", nullable: false),
                    SuperiorLevels = table.Column<int>(type: "int", nullable: false),
                    SubordinateLevels = table.Column<int>(type: "int", nullable: false),
                    IncludePeers = table.Column<bool>(type: "bit", nullable: false),
                    WeightSelf = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WeightSuperior = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WeightSubordinate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WeightPeer = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_RaterDirectionConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perf_Goal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ParentGoalId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    TargetUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_Goal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_Goal_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perf_Goal_Perf_Goal_ParentGoalId",
                        column: x => x.ParentGoalId,
                        principalTable: "Perf_Goal",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Perf_GradeBand",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MinPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SalaryIncreasePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BonusPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RequiresJustification = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_GradeBand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_GradeBand_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_RatingScaleDescription",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    ScorePoint = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_RatingScaleDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_RatingScaleDescription_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_EvaluationInstance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    EvaluationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotEmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    SnapshotEmpName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SnapshotPositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SnapshotOrganizationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SnapshotOrganizationName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinalScorePercent = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    FinalGrade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    IsMeritApplied = table.Column<bool>(type: "bit", nullable: false),
                    MeritAppliedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_EvaluationInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_EvaluationInstance_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perf_EvaluationInstance_Perf_EvaluationType_EvaluationTypeId",
                        column: x => x.EvaluationTypeId,
                        principalTable: "Perf_EvaluationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perf_Topic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_Topic", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_Topic_Perf_EvaluationType_EvaluationTypeId",
                        column: x => x.EvaluationTypeId,
                        principalTable: "Perf_EvaluationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_EvaluationAssignment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    EvaluationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    RaterDirectionConfigId = table.Column<long>(type: "bigint", nullable: false),
                    TargetScope = table.Column<int>(type: "int", nullable: false),
                    TargetOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TargetPosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    TargetHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_EvaluationAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_EvaluationAssignment_Perf_EvaluationPeriod_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "Perf_EvaluationPeriod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perf_EvaluationAssignment_Perf_EvaluationType_EvaluationTypeId",
                        column: x => x.EvaluationTypeId,
                        principalTable: "Perf_EvaluationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perf_EvaluationAssignment_Perf_RaterDirectionConfig_RaterDirectionConfigId",
                        column: x => x.RaterDirectionConfigId,
                        principalTable: "Perf_RaterDirectionConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_GoalCheckIn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<long>(type: "bigint", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueAtCheckIn = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_GoalCheckIn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_GoalCheckIn_Perf_Goal_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Perf_Goal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_RaterAssignment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    RaterHremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    WeightInFinalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Strengths = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AreasToImprove = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EthicsRating = table.Column<int>(type: "int", nullable: true),
                    QualityJustification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuantityJustification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EfficiencyJustification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_RaterAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_RaterAssignment_Perf_EvaluationInstance_EvaluationInstanceId",
                        column: x => x.EvaluationInstanceId,
                        principalTable: "Perf_EvaluationInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_SubTopic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_SubTopic", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_SubTopic_Perf_Topic_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Perf_Topic",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_Indicator",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubTopicId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OkrGoalId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_Indicator", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_Indicator_Perf_Goal_OkrGoalId",
                        column: x => x.OkrGoalId,
                        principalTable: "Perf_Goal",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Perf_Indicator_Perf_SubTopic_SubTopicId",
                        column: x => x.SubTopicId,
                        principalTable: "Perf_SubTopic",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perf_Score",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RaterAssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    ScorePoint = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perf_Score", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perf_Score_Perf_Indicator_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Perf_Indicator",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perf_Score_Perf_RaterAssignment_RaterAssignmentId",
                        column: x => x.RaterAssignmentId,
                        principalTable: "Perf_RaterAssignment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationAssignment_EvaluationPeriodId",
                table: "Perf_EvaluationAssignment",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationAssignment_EvaluationTypeId",
                table: "Perf_EvaluationAssignment",
                column: "EvaluationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationAssignment_RaterDirectionConfigId",
                table: "Perf_EvaluationAssignment",
                column: "RaterDirectionConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationInstance_EvaluationPeriodId_HremployeeId",
                table: "Perf_EvaluationInstance",
                columns: new[] { "EvaluationPeriodId", "HremployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationInstance_EvaluationTypeId",
                table: "Perf_EvaluationInstance",
                column: "EvaluationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_EvaluationInstance_JobMasterId",
                table: "Perf_EvaluationInstance",
                column: "JobMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Goal_EvaluationPeriodId",
                table: "Perf_Goal",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Goal_ParentGoalId",
                table: "Perf_Goal",
                column: "ParentGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_GoalCheckIn_GoalId",
                table: "Perf_GoalCheckIn",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_GradeBand_EvaluationPeriodId",
                table: "Perf_GradeBand",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Indicator_OkrGoalId",
                table: "Perf_Indicator",
                column: "OkrGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Indicator_SubTopicId",
                table: "Perf_Indicator",
                column: "SubTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_RaterAssignment_EvaluationInstanceId",
                table: "Perf_RaterAssignment",
                column: "EvaluationInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_RaterAssignment_RaterHremployeeId",
                table: "Perf_RaterAssignment",
                column: "RaterHremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_RatingScaleDescription_EvaluationPeriodId",
                table: "Perf_RatingScaleDescription",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Score_IndicatorId",
                table: "Perf_Score",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Score_RaterAssignmentId_IndicatorId",
                table: "Perf_Score",
                columns: new[] { "RaterAssignmentId", "IndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perf_SubTopic_TopicId",
                table: "Perf_SubTopic",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Topic_EvaluationTypeId",
                table: "Perf_Topic",
                column: "EvaluationTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perf_EvaluationAssignment");

            migrationBuilder.DropTable(
                name: "Perf_GoalCheckIn");

            migrationBuilder.DropTable(
                name: "Perf_GradeBand");

            migrationBuilder.DropTable(
                name: "Perf_RatingScaleDescription");

            migrationBuilder.DropTable(
                name: "Perf_Score");

            migrationBuilder.DropTable(
                name: "Perf_RaterDirectionConfig");

            migrationBuilder.DropTable(
                name: "Perf_Indicator");

            migrationBuilder.DropTable(
                name: "Perf_RaterAssignment");

            migrationBuilder.DropTable(
                name: "Perf_Goal");

            migrationBuilder.DropTable(
                name: "Perf_SubTopic");

            migrationBuilder.DropTable(
                name: "Perf_EvaluationInstance");

            migrationBuilder.DropTable(
                name: "Perf_Topic");

            migrationBuilder.DropTable(
                name: "Perf_EvaluationPeriod");

            migrationBuilder.DropTable(
                name: "Perf_EvaluationType");
        }
    }
}
