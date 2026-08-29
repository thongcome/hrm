using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRehireAndHireApprovalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HireJobMasterId",
                table: "Rec_Offer",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCard",
                table: "Rec_Candidate",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            // defaultValue: true — every existing employee row is currently
            // considered active (no prior column to derive this from), and
            // backfilling to false would incorrectly block every current
            // employee from EmployeeStatusHelper.CanTransact the moment this
            // migration runs.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "HREMPLOYEE",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriorResignDate",
                table: "Hrd_EmploymentHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriorWorkDate",
                table: "Hrd_EmploymentHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenureTreatment",
                table: "Hrd_EmploymentHistory",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pos_PositionSlot_his",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionSlotId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    PreviousEmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    NewHremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    NewEmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_PositionSlot_his", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pos_PositionSlot_his_Pos_PositionSlot_PositionSlotId",
                        column: x => x.PositionSlotId,
                        principalTable: "Pos_PositionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pos_PositionSlot_his_PositionSlotId",
                table: "Pos_PositionSlot_his",
                column: "PositionSlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pos_PositionSlot_his");

            migrationBuilder.DropColumn(
                name: "HireJobMasterId",
                table: "Rec_Offer");

            migrationBuilder.DropColumn(
                name: "IdCard",
                table: "Rec_Candidate");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "PriorResignDate",
                table: "Hrd_EmploymentHistory");

            migrationBuilder.DropColumn(
                name: "PriorWorkDate",
                table: "Hrd_EmploymentHistory");

            migrationBuilder.DropColumn(
                name: "TenureTreatment",
                table: "Hrd_EmploymentHistory");
        }
    }
}
