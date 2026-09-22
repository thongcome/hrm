using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // Skill_Category.CompanyId was still nvarchar(6) — every other CompanyId string column in the
    // database was widened to 50 already (CLAUDE.md "String columns are sized generously"), this one
    // was missed because it has no EF model class (dead scaffold, 0 rows) and never surfaced in that
    // pass. Found while building tools/deploy/20_new_customer.sql, whose generic column scan treats
    // it as the narrowest company-code column in the whole database and would cap every future
    // customer's company code at 6 characters for no reason. 0 rows, one unique index
    // (UXC_Skill_Category_CompanyId_Code) that stays well under the key-size limit after widening.
    public partial class WidenSkillCategoryCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Skill_Category ALTER COLUMN CompanyId nvarchar(50) NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // narrowing back to 6 could truncate data written since; leave widened.
        }
    }
}
