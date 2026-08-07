using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REC_ADMIN covers every internal Recruitment page
            // (/rec/requisitions, /rec/postings, /rec/pipeline/*,
            // /rec/interviews/*, /rec/candidates/*, /rec/offers*,
            // /rec/dashboard) — same "one menucode, many routes" pattern as
            // PAY_ADMIN. The public /careers* pages deliberately have no
            // menu/role_menu row at all (no [Authorize] on those pages).
            // menuid 45 = next after 44, rolemenuid 48 = next after 47 (both
            // verified against live DB before writing this).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[] { 45, "สรรหาบุคลากร (Recruitment)", "Recruitment", 1, true, 55, "REC_ADMIN", true, "/rec/requisitions", 1, true });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[] { 48, 45, 9, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 48 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 45 });
        }
    }
}
