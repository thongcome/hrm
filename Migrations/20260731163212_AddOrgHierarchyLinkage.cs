using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgHierarchyLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "HREMPLOYEE",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "orgcode",
                table: "HREMPLOYEE",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "orgcodefull",
                table: "HREMPLOYEE",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "orgcodefull",
                table: "com_organization",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Backfill com_organization.orgcodefull for every existing row:
            // 2 digits per level, no delimiter, walked from the root
            // (istop=1) down through parent_code. Verified against the live
            // 12-row tree before being baked in here (see the plan file's
            // note on this migration for the confirmed output). Re-run this
            // same computation (not a hand-edit) if the org tree is ever
            // restructured — don't just patch individual rows.
            migrationBuilder.Sql(@"
;WITH OrgTree AS (
    SELECT id, code, parent_code,
           CAST(RIGHT('00' + CAST(ROW_NUMBER() OVER (PARTITION BY parent_code ORDER BY id) AS varchar(2)), 2) AS varchar(100)) AS orgcodefull
    FROM com_organization
    WHERE istop = 1
    UNION ALL
    SELECT c.id, c.code, c.parent_code,
           CAST(t.orgcodefull + RIGHT('00' + CAST(ROW_NUMBER() OVER (PARTITION BY c.parent_code ORDER BY c.id) AS varchar(2)), 2) AS varchar(100))
    FROM com_organization c
    JOIN OrgTree t ON c.parent_code = t.code
)
UPDATE o SET o.orgcodefull = t.orgcodefull
FROM com_organization o
JOIN OrgTree t ON o.id = t.id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "orgcode",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "orgcodefull",
                table: "HREMPLOYEE");

            migrationBuilder.DropColumn(
                name: "orgcodefull",
                table: "com_organization");
        }
    }
}
