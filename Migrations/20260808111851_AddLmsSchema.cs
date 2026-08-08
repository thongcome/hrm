using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLmsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lms_Course",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryType = table.Column<int>(type: "int", nullable: false),
                    DurationHours = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    PassingScorePercent = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_Course", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_CourseCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_CourseCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_CourseSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    SessionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OnlineLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxSeats = table.Column<int>(type: "int", nullable: true),
                    ActualCost = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_CourseSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_Enrollment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseSessionId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuizScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SourceDevelopmentActionId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_Enrollment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_QuizAnswer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizAttemptId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    SelectedChoice = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_QuizAnswer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_QuizAttempt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_QuizAttempt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_QuizQuestion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChoiceA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChoiceB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChoiceC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChoiceD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CorrectChoice = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_QuizQuestion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_TrainingBudget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_TrainingBudget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lms_TrainingNeed",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedTopic = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SourceCompetencyGapId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lms_TrainingNeed", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lms_Course");

            migrationBuilder.DropTable(
                name: "Lms_CourseCategory");

            migrationBuilder.DropTable(
                name: "Lms_CourseSession");

            migrationBuilder.DropTable(
                name: "Lms_Enrollment");

            migrationBuilder.DropTable(
                name: "Lms_QuizAnswer");

            migrationBuilder.DropTable(
                name: "Lms_QuizAttempt");

            migrationBuilder.DropTable(
                name: "Lms_QuizQuestion");

            migrationBuilder.DropTable(
                name: "Lms_TrainingBudget");

            migrationBuilder.DropTable(
                name: "Lms_TrainingNeed");
        }
    }
}
