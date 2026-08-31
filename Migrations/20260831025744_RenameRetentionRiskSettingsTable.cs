using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class RenameRetentionRiskSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Talent_RetentionRiskSettingsList",
                table: "Talent_RetentionRiskSettingsList");

            migrationBuilder.RenameTable(
                name: "Talent_RetentionRiskSettingsList",
                newName: "Talent_RetentionRiskSettings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Talent_RetentionRiskSettings",
                table: "Talent_RetentionRiskSettings",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Talent_RetentionRiskSettings",
                table: "Talent_RetentionRiskSettings");

            migrationBuilder.RenameTable(
                name: "Talent_RetentionRiskSettings",
                newName: "Talent_RetentionRiskSettingsList");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Talent_RetentionRiskSettingsList",
                table: "Talent_RetentionRiskSettingsList",
                column: "Id");
        }
    }
}
