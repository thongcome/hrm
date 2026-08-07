using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eng_ActionPlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CampaignId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: false),
                    ImpactedOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TargetCompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_ActionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eng_QuestionTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_QuestionTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eng_Recognition",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    FromHremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ToHremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CoreValueTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_Recognition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eng_SurveyCampaign",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RelaunchedFromCampaignId = table.Column<long>(type: "bigint", nullable: true),
                    InvitedCount = table.Column<int>(type: "int", nullable: false),
                    ResponseCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_SurveyCampaign", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eng_ActionPlanMilestones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_ActionPlanMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eng_ActionPlanMilestones_Eng_ActionPlan_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "Eng_ActionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eng_CampaignQuestion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    SourceTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_CampaignQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eng_CampaignQuestion_Eng_SurveyCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Eng_SurveyCampaign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eng_CampaignTarget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TargetHremployeeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_CampaignTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eng_CampaignTarget_Eng_SurveyCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Eng_SurveyCampaign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eng_SurveyResponse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NpsScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_SurveyResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eng_SurveyResponse_Eng_SurveyCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Eng_SurveyCampaign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eng_SurveyAnswer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseId = table.Column<long>(type: "bigint", nullable: false),
                    CampaignQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    RatingValue = table.Column<int>(type: "int", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChoiceValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YesNoValue = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_SurveyAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eng_SurveyAnswer_Eng_CampaignQuestion_CampaignQuestionId",
                        column: x => x.CampaignQuestionId,
                        principalTable: "Eng_CampaignQuestion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eng_SurveyAnswer_Eng_SurveyResponse_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Eng_SurveyResponse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eng_ActionPlanMilestones_ActionPlanId",
                table: "Eng_ActionPlanMilestones",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_CampaignQuestion_CampaignId",
                table: "Eng_CampaignQuestion",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_CampaignTarget_CampaignId",
                table: "Eng_CampaignTarget",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_SurveyAnswer_CampaignQuestionId",
                table: "Eng_SurveyAnswer",
                column: "CampaignQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_SurveyAnswer_ResponseId",
                table: "Eng_SurveyAnswer",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_SurveyResponse_CampaignId",
                table: "Eng_SurveyResponse",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eng_ActionPlanMilestones");

            migrationBuilder.DropTable(
                name: "Eng_CampaignTarget");

            migrationBuilder.DropTable(
                name: "Eng_QuestionTemplate");

            migrationBuilder.DropTable(
                name: "Eng_Recognition");

            migrationBuilder.DropTable(
                name: "Eng_SurveyAnswer");

            migrationBuilder.DropTable(
                name: "Eng_ActionPlan");

            migrationBuilder.DropTable(
                name: "Eng_CampaignQuestion");

            migrationBuilder.DropTable(
                name: "Eng_SurveyResponse");

            migrationBuilder.DropTable(
                name: "Eng_SurveyCampaign");
        }
    }
}
