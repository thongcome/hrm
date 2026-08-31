using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDanglingLmsTrainingNeedGapField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCompetencyGapId",
                table: "Lms_TrainingNeed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SourceCompetencyGapId",
                table: "Lms_TrainingNeed",
                type: "bigint",
                nullable: true);
        }
    }
}
