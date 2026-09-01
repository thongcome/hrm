using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddJobDescriptionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Job_ProfileDuties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,1)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IncludeInCompetency = table.Column<bool>(type: "bit", nullable: false),
                    LinkedCompetencyId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_ProfileDuties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_ProfileDuties_Comp_Competency_LinkedCompetencyId",
                        column: x => x.LinkedCompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Job_ProfileQualifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    QualType = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IncludeInCompetency = table.Column<bool>(type: "bit", nullable: false),
                    LinkedCompetencyId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_ProfileQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_ProfileQualifications_Comp_Competency_LinkedCompetencyId",
                        column: x => x.LinkedCompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileDuties_LinkedCompetencyId",
                table: "Job_ProfileDuties",
                column: "LinkedCompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileDuties_PosExecTypeId",
                table: "Job_ProfileDuties",
                column: "PosExecTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileQualifications_LinkedCompetencyId",
                table: "Job_ProfileQualifications",
                column: "LinkedCompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileQualifications_PosExecTypeId",
                table: "Job_ProfileQualifications",
                column: "PosExecTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Job_ProfileDuties");

            migrationBuilder.DropTable(
                name: "Job_ProfileQualifications");
        }
    }
}
