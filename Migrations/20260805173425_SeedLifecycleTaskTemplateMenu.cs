using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLifecycleTaskTemplateMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 31 = next after 30; rolemenuid 31 = next after 30
            // (re-verified against live DB immediately before writing this).
            // Reuses the existing PAY_ADMIN menucode (already granted to
            // Admin role via prior menu rows) rather than minting a new one —
            // same pattern as every other /pay/admin/* config page.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 31, "Checklist Onboarding/Offboarding", "Onboarding/Offboarding Checklist", 1, true, 41, "PAY_ADMIN", true, "/pay/admin/lifecycle-task-templates", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 31, 31, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 31 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 31 });
        }
    }
}
