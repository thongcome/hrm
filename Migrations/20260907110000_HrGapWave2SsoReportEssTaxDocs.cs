using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>
    /// HR gap wave 2 (REQ-125 SSO contribution report, REQ-134 ESS tax documents).
    /// No schema change — the SSO report lives in the Report Center and the ESS
    /// 50 ทวิ download is a route; only the ESS menu row is data.
    /// </summary>
    public partial class HrGapWave2SsoReportEssTaxDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/tax-documents')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'เอกสารภาษีของฉัน (50 ทวิ)', 'My Tax Documents', menulevel, isfinal, 45, menucode, isshow, '/ess/tax-documents', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/payslips';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/tax-documents';");
        }
    }
}
