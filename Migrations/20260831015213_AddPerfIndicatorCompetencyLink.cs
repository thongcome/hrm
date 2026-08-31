using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfIndicatorCompetencyLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompetencyId",
                table: "Perf_Indicator",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perf_Indicator_CompetencyId",
                table: "Perf_Indicator",
                column: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Perf_Indicator_Comp_Competency_CompetencyId",
                table: "Perf_Indicator",
                column: "CompetencyId",
                principalTable: "Comp_Competency",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Perf_Indicator_Comp_Competency_CompetencyId",
                table: "Perf_Indicator");

            migrationBuilder.DropIndex(
                name: "IX_Perf_Indicator_CompetencyId",
                table: "Perf_Indicator");

            migrationBuilder.DropColumn(
                name: "CompetencyId",
                table: "Perf_Indicator");
        }
    }
}
