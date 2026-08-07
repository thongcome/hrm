using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rec_Application",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    JobPostingId = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RejectedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Application", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_Candidate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResumeDocCenterId = table.Column<long>(type: "bigint", nullable: true),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Candidate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_CandidateDataRetentionSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    RetentionMonths = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_CandidateDataRetentionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_Interview",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    Round = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Interview", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_InterviewPanelist",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    IsLead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_InterviewPanelist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_InterviewScorecard",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    OverallRecommendation = table.Column<int>(type: "int", nullable: true),
                    Strengths = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Concerns = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_InterviewScorecard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_JobPosting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_JobPosting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_Offer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    TargetPositionSlotId = table.Column<long>(type: "bigint", nullable: false),
                    OfferedSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HiredHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Offer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_Requisition",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    RequisitionType = table.Column<int>(type: "int", nullable: false),
                    TargetPositionSlotId = table.Column<long>(type: "bigint", nullable: true),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    OpeningsCount = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposedMinSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ProposedMaxSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    NewPositionSlotId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Requisition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_ScorecardCompetencyRatings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScorecardId = table.Column<long>(type: "bigint", nullable: false),
                    CompetencyId = table.Column<long>(type: "bigint", nullable: false),
                    RatedLevel = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_ScorecardCompetencyRatings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rec_Application");

            migrationBuilder.DropTable(
                name: "Rec_Candidate");

            migrationBuilder.DropTable(
                name: "Rec_CandidateDataRetentionSettings");

            migrationBuilder.DropTable(
                name: "Rec_Interview");

            migrationBuilder.DropTable(
                name: "Rec_InterviewPanelist");

            migrationBuilder.DropTable(
                name: "Rec_InterviewScorecard");

            migrationBuilder.DropTable(
                name: "Rec_JobPosting");

            migrationBuilder.DropTable(
                name: "Rec_Offer");

            migrationBuilder.DropTable(
                name: "Rec_Requisition");

            migrationBuilder.DropTable(
                name: "Rec_ScorecardCompetencyRatings");
        }
    }
}
