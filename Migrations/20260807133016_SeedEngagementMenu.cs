using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedEngagementMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-verified against live DB immediately before writing this:
            // menuid max=49, rolemenuid max=53.

            // Single menucode ENG_ADMIN reused across all 4 admin pages (same
            // pattern as JOBCOMP_ADMIN/PAY_ADMIN spanning many config pages)
            // — Admin role only. The 3 ESS pages (surveys/recognition) reuse
            // the existing ESS_ACCESS menucode already granted to Employee,
            // same as every other ESS_* page, so no new menu row for those.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 50, "ภาพรวม Employee Engagement", "Engagement Dashboard", 1, true, 51, "ENG_ADMIN", true, "/eng/dashboard", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 54, 50, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 54 });

            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 50 });
        }
    }
}
