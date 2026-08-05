using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeRelationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hr_DisciplinaryCases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleViolated = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SuspensionDays = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hr_DisciplinaryCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hr_DisciplinaryCases_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hr_Grievances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvolvedPersons = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<long>(type: "bigint", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hr_Grievances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hr_Grievances_HREMPLOYEE_ReporterHremployeeId",
                        column: x => x.ReporterHremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hr_DisciplinaryCases_HremployeeId",
                table: "Hr_DisciplinaryCases",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Hr_Grievances_ReporterHremployeeId",
                table: "Hr_Grievances",
                column: "ReporterHremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hr_DisciplinaryCases");

            migrationBuilder.DropTable(
                name: "Hr_Grievances");
        }
    }
}
