using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class JdKpiVersioningAndCloseLegacyMenuGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JdReviewedBy",
                table: "Pos_ExecType",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JdReviewedDate",
                table: "Pos_ExecType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JdVersion",
                table: "Pos_ExecType",
                type: "int",
                nullable: false,
                defaultValue: 1); // matches the model default — every existing profile starts at version 1, not 0

            migrationBuilder.CreateTable(
                name: "Job_ProfileKpis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TargetDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_ProfileKpis", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileKpis_PosExecTypeId",
                table: "Job_ProfileKpis",
                column: "PosExecTypeId");

            // Menu gap-closure (CEO queue item 2, 1 ก.ย. 2569): the seeder
            // never touches menucode on existing sc_menu rows (grants are
            // human territory), so the previously-ungated rows are gated
            // here exactly once — and the three DEAD legacy links (no @page
            // owns those routes; they 404) are hidden outright. Pages got
            // matching [Authorize(Policy = "Menu:...")] in the same commit.
            migrationBuilder.Sql(@"
UPDATE sc_menu SET menucode='SYS_ADMIN' WHERE CAST(url AS nvarchar(500)) IN (N'/dev/pages',N'/masterdata',N'/sc_users') AND menucode IS NULL;
UPDATE sc_menu SET menucode='PAY_ADMIN' WHERE CAST(url AS nvarchar(500)) IN (N'/hrpayrolldasboard',N'/hremployees',N'/AIpayrollprocess',N'/hrpayrolls') AND menucode IS NULL;
UPDATE sc_menu SET isshow=0, isactive=0, modby='CloseLegacyMenuGaps' WHERE CAST(url AS nvarchar(500)) IN (N'/income',N'/ot',N'/onlinereport');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Job_ProfileKpis");

            migrationBuilder.DropColumn(
                name: "JdReviewedBy",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "JdReviewedDate",
                table: "Pos_ExecType");

            migrationBuilder.DropColumn(
                name: "JdVersion",
                table: "Pos_ExecType");
        }
    }
}
