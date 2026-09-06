using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>
    /// Employee asset custody module (reuses the legacy epms tables asset_owner /
    /// asset_notice already in the schema — no table change). Only the two menu
    /// rows are data: HR registry (/asset/registry, menucode ASSET_ADMIN) and the
    /// ESS "my assets" page (/ess/my-assets, ESS_ACCESS).
    /// </summary>
    public partial class AssetCustodyMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/asset/registry')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'ทะเบียนทรัพย์สินที่มอบให้พนักงาน', 'Employee Asset Custody', menulevel, isfinal, 30, 'ASSET_ADMIN', isshow, '/asset/registry', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/employee/personnel-profile';
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/my-assets')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'ทรัพย์สินของฉัน', 'My Assets', menulevel, isfinal, 55, menucode, isshow, '/ess/my-assets', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/payslips';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) IN ('/asset/registry','/ess/my-assets');");
        }
    }
}
