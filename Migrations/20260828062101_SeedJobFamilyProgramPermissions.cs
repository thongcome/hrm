using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedJobFamilyProgramPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Proof-of-concept for the new Program:XXX per-action permission
            // layer (see Services/Login/ProgramAuthorization.cs, wired into
            // Components/Pages/Job/JobFamilyAdmin.razor) — sc_program/
            // sc_role_program were both empty (verified against live DB
            // immediately before writing this, 2026-08-28) so progid/roleprogid
            // start at 1. progname follows the "{entity label} — {action
            // label}" convention PermissionAdmin.razor's grouping logic parses.
            migrationBuilder.InsertData(
                table: "sc_program",
                columns: new[] { "progid", "progname", "progcode", "isactive" },
                values: new object[,]
                {
                    { 1L, "สายอาชีพ (Job Family) — เพิ่ม", "JOBFAMILY_CREATE", true },
                    { 2L, "สายอาชีพ (Job Family) — แก้ไข", "JOBFAMILY_EDIT", true },
                });

            // Granted to Admin (roleid=9, confirmed against live DB) by
            // default — matches the same default-Admin-only grant every new
            // sc_menu/sc_role_menu seed in this codebase uses.
            migrationBuilder.InsertData(
                table: "sc_role_program",
                columns: new[] { "roleprogid", "roleid", "progid", "isactive" },
                values: new object[,]
                {
                    { 1L, 9L, 1L, true },
                    { 2L, 9L, 2L, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_program", keyColumn: "roleprogid", keyValues: new object[] { 1L });
            migrationBuilder.DeleteData(table: "sc_role_program", keyColumn: "roleprogid", keyValues: new object[] { 2L });
            migrationBuilder.DeleteData(table: "sc_program", keyColumn: "progid", keyValues: new object[] { 1L });
            migrationBuilder.DeleteData(table: "sc_program", keyColumn: "progid", keyValues: new object[] { 2L });
        }
    }
}
