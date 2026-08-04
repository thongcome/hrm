using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedPerfAccessMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 28 = next after 27; rolemenuid 26/27 = next after 25
            // (all verified against live DB before writing this). Granted to
            // both Employee (10) and Admin (9) so every employee can reach
            // their own scoring inbox — Admin included so HR/admin test
            // accounts (which aren't necessarily linked to an Hremployee with
            // rater assignments) can still open the page while testing.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 28, "งานประเมินของฉัน", "My Evaluations", 1, true, 38, "PERF_ACCESS", true, "/perf/my-evaluations", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 26, 28, 10, true },
                    { 27, 28, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 26 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 27 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 28 });
        }
    }
}
