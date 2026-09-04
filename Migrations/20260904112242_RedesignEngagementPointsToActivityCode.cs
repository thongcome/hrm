using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class RedesignEngagementPointsToActivityCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Eng_PointsRule");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Eng_PointsLedger");

            migrationBuilder.AddColumn<string>(
                name: "ActivityCode",
                table: "Eng_PointsRule",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActivityName",
                table: "Eng_PointsRule",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivityCode",
                table: "Eng_PointsLedger",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityCode",
                table: "Eng_PointsRule");

            migrationBuilder.DropColumn(
                name: "ActivityName",
                table: "Eng_PointsRule");

            migrationBuilder.DropColumn(
                name: "ActivityCode",
                table: "Eng_PointsLedger");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Eng_PointsRule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Eng_PointsLedger",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
