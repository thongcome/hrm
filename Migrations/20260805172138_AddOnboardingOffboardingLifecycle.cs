using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingOffboardingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultProbationDays",
                table: "Pay_PayslipSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PROBATION_CONFIRMED_DATE",
                table: "HREMPLOYEE",
                type: "DATE",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PROBATION_END_DATE",
                table: "HREMPLOYEE",
                type: "DATE",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Hrd_ExitInterview",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    InterviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConductedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReasonCode = table.Column<int>(type: "int", nullable: false),
                    ReasonNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WouldRecommendCompany = table.Column<bool>(type: "bit", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_ExitInterview", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_LifecycleTaskInstance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssetDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_LifecycleTaskInstance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_LifecycleTaskTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultAssigneeRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_LifecycleTaskTemplate", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                column: "DefaultProbationDays",
                value: 119);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hrd_ExitInterview");

            migrationBuilder.DropTable(
                name: "Hrd_LifecycleTaskInstance");

            migrationBuilder.DropTable(
                name: "Hrd_LifecycleTaskTemplate");

            migrationBuilder.DropColumn(
                name: "DefaultProbationDays",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "PROBATION_CONFIRMED_DATE",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "PROBATION_END_DATE",
                table: "HREMPLOYEE");
        }
    }
}
