using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddSeparationDateChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hr_SeparationDateChange",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeparationRequestId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OldEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hr_SeparationDateChange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hr_SeparationDateChange_Hr_SeparationRequest_SeparationRequestId",
                        column: x => x.SeparationRequestId,
                        principalTable: "Hr_SeparationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hr_SeparationDateChange_HremployeeId_Status",
                table: "Hr_SeparationDateChange",
                columns: new[] { "HremployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Hr_SeparationDateChange_JobMasterId",
                table: "Hr_SeparationDateChange",
                column: "JobMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Hr_SeparationDateChange_SeparationRequestId_Status",
                table: "Hr_SeparationDateChange",
                columns: new[] { "SeparationRequestId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hr_SeparationDateChange");
        }
    }
}
