using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOkrV2Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Okr_Cycle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_Cycle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Okr_GoalCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_GoalCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Okr_Objective",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ParentObjectiveId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_Objective", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Okr_Objective_Okr_Cycle_CycleId",
                        column: x => x.CycleId,
                        principalTable: "Okr_Cycle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Okr_Objective_Okr_GoalCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Okr_GoalCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Okr_Objective_Okr_Objective_ParentObjectiveId",
                        column: x => x.ParentObjectiveId,
                        principalTable: "Okr_Objective",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Okr_KeyResult",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetricType = table.Column<int>(type: "int", nullable: false),
                    StartValue = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_KeyResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Okr_KeyResult_Okr_Objective_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Okr_Objective",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Okr_ObjectiveUpdate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusAtUpdate = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_ObjectiveUpdate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Okr_ObjectiveUpdate_Okr_Objective_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Okr_Objective",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Okr_KeyResultCheckIn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyResultId = table.Column<long>(type: "bigint", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueAtCheckIn = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okr_KeyResultCheckIn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Okr_KeyResultCheckIn_Okr_KeyResult_KeyResultId",
                        column: x => x.KeyResultId,
                        principalTable: "Okr_KeyResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Okr_KeyResult_ObjectiveId",
                table: "Okr_KeyResult",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Okr_KeyResultCheckIn_KeyResultId",
                table: "Okr_KeyResultCheckIn",
                column: "KeyResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Okr_Objective_CategoryId",
                table: "Okr_Objective",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Okr_Objective_CycleId",
                table: "Okr_Objective",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Okr_Objective_ParentObjectiveId",
                table: "Okr_Objective",
                column: "ParentObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Okr_ObjectiveUpdate_ObjectiveId",
                table: "Okr_ObjectiveUpdate",
                column: "ObjectiveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Okr_KeyResultCheckIn");

            migrationBuilder.DropTable(
                name: "Okr_ObjectiveUpdate");

            migrationBuilder.DropTable(
                name: "Okr_KeyResult");

            migrationBuilder.DropTable(
                name: "Okr_Objective");

            migrationBuilder.DropTable(
                name: "Okr_Cycle");

            migrationBuilder.DropTable(
                name: "Okr_GoalCategory");
        }
    }
}
