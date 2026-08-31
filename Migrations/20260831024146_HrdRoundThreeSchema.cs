using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class HrdRoundThreeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Career_PathTransition",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    FromPosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    ToPosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Career_PathTransition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Talent_RetentionRiskSettingsList",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NewHireMonthsThreshold = table.Column<int>(type: "int", nullable: false),
                    NewHireWeight = table.Column<int>(type: "int", nullable: false),
                    StagnationMonthsThreshold = table.Column<int>(type: "int", nullable: false),
                    StagnationWeight = table.Column<int>(type: "int", nullable: false),
                    HighPerformerScorePercent = table.Column<int>(type: "int", nullable: false),
                    HighPerformerWeight = table.Column<int>(type: "int", nullable: false),
                    HighRiskScoreThreshold = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Talent_RetentionRiskSettingsList", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Career_PathTransition");

            migrationBuilder.DropTable(
                name: "Talent_RetentionRiskSettingsList");
        }
    }
}
