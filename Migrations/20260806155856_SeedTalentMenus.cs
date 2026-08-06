using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedTalentMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 43-44 = next after 42; rolemenuid 45-47 = next after 44
            // (all verified against live DB before writing this).
            // TALENT_ACCESS -> /talent/team-rating, granted to both Employee
            // (10, managers rating their own team) and Admin (9, testing) —
            // same pattern as IDP_ACCESS/PERF_ACCESS. /talent/settings and
            // /talent/pool share TALENT_ADMIN without their own sc_menu row
            // (mirrors JobProfileDetail.razor's Menu:JOBCOMP_ADMIN reuse).
            // TALENT_ADMIN -> /talent/nine-box, Admin (9) only.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 43, "ให้คะแนนศักยภาพทีม", "Rate My Team's Potential", 1, true, 53, "TALENT_ACCESS", true, "/talent/team-rating", 1, true },
                    { 44, "9-Box Grid (Talent Management)", "9-Box Grid", 1, true, 54, "TALENT_ADMIN", true, "/talent/nine-box", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 45, 43, 10, true },
                    { 46, 43, 9, true },
                    { 47, 44, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 45 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 46 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 47 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 43 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 44 });
        }
    }
}
