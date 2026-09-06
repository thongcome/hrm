using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddScRolePosExecCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Role-model expansion (CEO, 2026-09-07): auto-grant a role by
            // position/rank (Pos_ExecType.Code, e.g. "A03") the same way
            // employeetype_code already auto-grants by employee type —
            // continuously reconciled by DerivedRoleSyncService, not a
            // one-time bootstrap. Mirrors the employeetype_code column added
            // in Step3RoleMappingAndRenameDemoCompany exactly.
            migrationBuilder.AddColumn<string>(
                name: "pos_exec_code",
                table: "sc_role",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pos_exec_code",
                table: "sc_role");
        }
    }
}
