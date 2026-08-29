using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionSlotEmpNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmpNo",
                table: "Pos_PositionSlot",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            // Backfill EmpNo for slots already occupied — otherwise the
            // snapshot stays null until each slot is next saved through
            // EmployeePositionSync.SyncAsync.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.EmpNo = h.EMP_NO
                FROM Pos_PositionSlot p
                JOIN HREMPLOYEE h ON h.ID = p.HremployeeId
                WHERE p.HremployeeId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpNo",
                table: "Pos_PositionSlot");
        }
    }
}
