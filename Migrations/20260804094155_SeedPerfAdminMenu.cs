using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedPerfAdminMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 27 = next after 26 (latest existing row, verified against
            // live DB before writing this); roleid 9 = "Admin"; rolemenuid 25
            // = next after 24; menugroupid 1 = same group every other menu
            // row uses. Single entry point — the config/assignment/instance/
            // hr-dashboard/goals pages all share this one menucode, matching
            // the PAY_ADMIN precedent (many pages, one menucode).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 27, "ประเมินผลปฏิบัติงาน (Performance/KPI)", "Performance Management", 1, true, 37, "PERF_ADMIN", true, "/perf/periods", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 25, 27, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 25 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 27 });
        }
    }
}
