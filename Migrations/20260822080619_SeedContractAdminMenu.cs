using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedContractAdminMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CT_CONTRACT_ADMIN covers all 3 new pages (/contracts,
            // /contracts/currencies, /contracts/warranty-types) under one
            // menucode, matching the PAY_ADMIN/PERF_ADMIN precedent of not
            // seeding a separate menu per sub-page. Admin-role-only, same as
            // most CRUD-coverage additions this session. menuid 60 = next
            // after 59, rolemenuid 67 = next after 66 (both verified against
            // live DB via a temporary diagnostic query before writing this).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[] { 60, "สัญญา (Contracts)", "Contracts", 1, true, 60, "CT_CONTRACT_ADMIN", true, "/contracts", 1, true });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[] { 67, 60, 9, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 67 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 60 });
        }
    }
}
