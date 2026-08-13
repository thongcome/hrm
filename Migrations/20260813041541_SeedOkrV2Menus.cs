using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedOkrV2Menus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 58-59 = next after 57; rolemenuid 64-66 = next after 63
            // (verified against live DB before writing this).
            // OKR_ADMIN -> /okr/tree, covers all admin pages in this module
            // via the same policy (Cycle/Category/Tree/Detail/Dashboard) —
            // same pattern as PERF_ADMIN. Admin (9) only.
            // OKR_ACCESS -> /ess/my-okr, Admin (9, testing) + Employee (10,
            // own objectives) — same pattern as PERF_ACCESS/ESS_ACCESS.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 58, "OKR เป้าหมายเชิงกลยุทธ์ (v2)", "OKR v2", 1, true, 60, "OKR_ADMIN", true, "/okr/tree", 1, true },
                    { 59, "OKR ของฉัน", "My OKR", 1, true, 61, "OKR_ACCESS", true, "/ess/my-okr", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 64, 58, 9, true },
                    { 65, 59, 9, true },
                    { 66, 59, 10, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 64 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 65 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 66 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 58 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 59 });
        }
    }
}
