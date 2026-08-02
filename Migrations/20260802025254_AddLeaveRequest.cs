using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lve_LeaveRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    LeaveType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lve_LeaveRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lve_LeaveRequest_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lve_LeaveRequest_HremployeeId",
                table: "Lve_LeaveRequest",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lve_LeaveRequest");
        }
    }
}
