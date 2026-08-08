using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgChangeRequestCompanyBranchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NewIsBranch",
                table: "Org_OrganizationChangeRequest",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewIsCompany",
                table: "Org_OrganizationChangeRequest",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewIsBranch",
                table: "Org_OrganizationChangeRequest");

            migrationBuilder.DropColumn(
                name: "NewIsCompany",
                table: "Org_OrganizationChangeRequest");
        }
    }
}
