using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuccessionMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SUCC_ADMIN covers both internal Succession Planning pages
            // (/succession/key-positions, /succession/bench-strength) —
            // Admin-role-only, matching the confidentiality precedent set by
            // TALENT_ADMIN/IDP_ADMIN/PERF_ADMIN (no separate "designated
            // executives" role exists in this system). menuid 46 = next
            // after 45 (SeedRecMenus), rolemenuid 49 = next after 48 (both
            // verified against live DB before writing this).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[] { 46, "แผนสืบทอดตำแหน่ง (Succession Planning)", "Succession Planning", 1, true, 56, "SUCC_ADMIN", true, "/succession/key-positions", 1, true });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[] { 49, 46, 9, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 49 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 46 });
        }
    }
}
