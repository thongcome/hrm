using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCompetencyManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CareerTrack",
                table: "Pos_ExecType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobFamilyId",
                table: "Pos_ExecType",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobLevelId",
                table: "Pos_ExecType",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyAccountabilities",
                table: "Pos_ExecType",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Pos_ExecType",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Comp_Category",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CategoryType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comp_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Job_Family",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_Family", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Job_Level",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CareerTrack = table.Column<int>(type: "int", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_Level", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comp_Competency",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comp_Competency", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comp_Competency_Comp_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Comp_Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comp_ProficiencyLevel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetencyId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comp_ProficiencyLevel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comp_ProficiencyLevel_Comp_Competency_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Job_CompetencyRequirements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CompetencyId = table.Column<long>(type: "bigint", nullable: false),
                    RequiredLevel = table.Column<int>(type: "int", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_CompetencyRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_CompetencyRequirements_Comp_Competency_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Comp_Competency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comp_Competency_CategoryId",
                table: "Comp_Competency",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Comp_ProficiencyLevel_CompetencyId",
                table: "Comp_ProficiencyLevel",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_CompetencyRequirements_CompetencyId",
                table: "Job_CompetencyRequirements",
                column: "CompetencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comp_ProficiencyLevel");

            migrationBuilder.DropTable(
                name: "Job_CompetencyRequirements");

            migrationBuilder.DropTable(
                name: "Job_Family");

            migrationBuilder.DropTable(
                name: "Job_Level");

            migrationBuilder.DropTable(
                name: "Comp_Competency");

            migrationBuilder.DropTable(
                name: "Comp_Category");

            migrationBuilder.DropColumn(
                name: "CareerTrack",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "JobFamilyId",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "JobLevelId",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "KeyAccountabilities",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Pos_ExecType");
        }
    }
}
