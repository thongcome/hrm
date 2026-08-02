using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionSalaryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PositionSalaryHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    OldPositionCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    OldPositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewPositionCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    NewPositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OldSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    NewSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    OrderNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PositionSalaryHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PositionSalaryHistory_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PositionSalaryHistory_HremployeeId",
                table: "Pay_PositionSalaryHistory",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_PositionSalaryHistory");
        }
    }
}
