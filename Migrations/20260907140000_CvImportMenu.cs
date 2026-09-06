using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>CV import page (/rec/import-cv) menu row — no schema change.</summary>
    public partial class CvImportMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/rec/import-cv')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'นำเข้า CV / เรซูเม่', 'Import CVs', menulevel, isfinal, 25, menucode, isshow, '/rec/import-cv', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/rec/postings';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/rec/import-cv';");
        }
    }
}
