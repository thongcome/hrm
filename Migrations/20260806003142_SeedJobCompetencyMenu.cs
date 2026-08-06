using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedJobCompetencyMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-verified against live DB immediately before writing this:
            // menuid max=36, rolemenuid max=37.

            // Single menucode JOBCOMP_ADMIN reused across all 4 pages (same
            // pattern as PAY_ADMIN spanning many config pages) — Admin role
            // only for now, this is a design-time config module for HR.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 37, "สายอาชีพ (Job Family)", "Job Family", 1, true, 47, "JOBCOMP_ADMIN", true, "/job/families", 1, true },
                    { 38, "ระดับสายอาชีพ (Job Level)", "Job Level", 1, true, 48, "JOBCOMP_ADMIN", true, "/job/levels", 1, true },
                    { 39, "หมวดสมรรถนะ", "Competency Category", 1, true, 49, "JOBCOMP_ADMIN", true, "/competency/categories", 1, true },
                    { 40, "คลังสมรรถนะ", "Competency Library", 1, true, 50, "JOBCOMP_ADMIN", true, "/competency/library", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 38, 37, 9, true },
                    { 39, 38, 9, true },
                    { 40, 39, 9, true },
                    { 41, 40, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 38 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 39 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 40 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 41 });

            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 37 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 38 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 39 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 40 });
        }
    }
}
