using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedKmMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 55-56 = next after 54; rolemenuid 60-62 = next after 59
            // (all verified against live DB before writing this).
            // KM_ADMIN -> /km/categories, Admin (9) only — /km/articles shares
            // this same policy without its own sc_menu row (mirrors
            // JobProfileDetail.razor's Menu:JOBCOMP_ADMIN reuse pattern).
            // KM_ACCESS -> /km/articles-list, granted to both Employee (10)
            // and Admin (9) — same pattern as ESS_ACCESS/IDP_ACCESS/
            // LMS_ACCESS. /km/articles/{id} and /km/experts share this same
            // policy too.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 55, "จัดการความรู้ (Knowledge Management)", "Knowledge Management Admin", 1, true, 62, "KM_ADMIN", true, "/km/categories", 1, true },
                    { 56, "คลังความรู้", "Knowledge Base", 1, true, 63, "KM_ACCESS", true, "/km/articles-list", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 60, 55, 9, true },
                    { 61, 56, 9, true },
                    { 62, 56, 10, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 60 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 61 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 62 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 55 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 56 });
        }
    }
}
