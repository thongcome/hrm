using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteFlagsForCrudSkillFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Sso_ClientRoleMapping",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Rec_CandidateExperience",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Rec_CandidateEducation",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Perf_RatingScaleDescription",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Perf_GradeBand",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Pay_ProvidentFundVestingTier",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Pay_ProvidentFundRateMatrixRule",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Lms_QuizQuestion",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Job_CompetencyRequirements",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Sso_ClientRoleMapping");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Rec_CandidateExperience");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Rec_CandidateEducation");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Perf_RatingScaleDescription");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Perf_GradeBand");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Pay_ProvidentFundVestingTier");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Pay_ProvidentFundRateMatrixRule");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Lms_QuizQuestion");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Job_CompetencyRequirements");
        }
    }
}
