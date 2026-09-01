using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SingleActiveCompanyOrgScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CEO rule (1 ก.ย. 2569): "บริษัท active ได้ 1" — exactly one
            // active com_company at a time; org pages key the visible tree by
            // comp_code == com_company.code. Data catch-up for that rule:
            //   1. The legacy 16-node tree (built when the system was single-
            //      company and comp_code was never stamped) belongs to the
            //      real company row code='AD'.
            //   2. Leave only the demo company ADVD active (the current demo
            //      focus); flipping the active company back is a one-switch
            //      action on the company admin page, which now enforces the
            //      single-active invariant itself.
            migrationBuilder.Sql(@"
UPDATE com_organization SET comp_code='AD' WHERE comp_code IS NULL;
UPDATE com_company SET isActive=0, moddate=GETDATE() WHERE code<>'ADVD' AND isActive=1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
