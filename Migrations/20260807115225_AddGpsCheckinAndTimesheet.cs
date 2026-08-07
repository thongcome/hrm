using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsCheckinAndTimesheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowGpsCheckin",
                table: "Att_CompanySetting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Att_GeofenceLocation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_GeofenceLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Att_Project",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Att_TimesheetSubmission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_TimesheetSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_TimesheetSubmission_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Att_TimesheetEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_TimesheetEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_TimesheetEntry_Att_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Att_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Att_TimesheetEntry_Att_TimesheetSubmission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Att_TimesheetSubmission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Att_TimesheetEntry_ProjectId",
                table: "Att_TimesheetEntry",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_TimesheetEntry_SubmissionId",
                table: "Att_TimesheetEntry",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_TimesheetSubmission_HremployeeId",
                table: "Att_TimesheetSubmission",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Att_GeofenceLocation");

            migrationBuilder.DropTable(
                name: "Att_TimesheetEntry");

            migrationBuilder.DropTable(
                name: "Att_Project");

            migrationBuilder.DropTable(
                name: "Att_TimesheetSubmission");

            migrationBuilder.DropColumn(
                name: "AllowGpsCheckin",
                table: "Att_CompanySetting");
        }
    }
}
