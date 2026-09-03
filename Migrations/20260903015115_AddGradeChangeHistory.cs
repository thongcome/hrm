using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pos_GradeChangeHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OldGradeCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    NewGradeCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OldPlevel = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    NewPlevel = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsPromotion = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ChangedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_GradeChangeHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pos_GradeChangeHistory_CompanyId",
                table: "Pos_GradeChangeHistory",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Pos_GradeChangeHistory_HremployeeId",
                table: "Pos_GradeChangeHistory",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pos_GradeChangeHistory");
        }
    }
}
