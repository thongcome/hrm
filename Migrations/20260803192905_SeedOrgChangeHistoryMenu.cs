using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedOrgChangeHistoryMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 26 = next after 25 (latest existing row); roleid 9 =
            // "Admin", rolemenuid 24 = next after 23; menugroupid 1 = same
            // group as the existing ORG_ADMIN menu row — confirmed against
            // live DB before writing this, not guessed.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 26, "ประวัติการปรับผังองค์กร", "Org Change History", 1, true, 36, "ORG_ADMIN", true, "/org/change-history", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 24, 26, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 24 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 26 });
        }
    }
}
