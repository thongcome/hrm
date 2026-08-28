using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ExtendToaForApproverDelegation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "toa",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentStoragePath",
                table: "toa",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "toa",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_toa_OrganizationId",
                table: "toa",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_toa_com_organization_OrganizationId",
                table: "toa",
                column: "OrganizationId",
                principalTable: "com_organization",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_toa_com_organization_OrganizationId",
                table: "toa");

            migrationBuilder.DropIndex(
                name: "IX_toa_OrganizationId",
                table: "toa");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "toa");

            migrationBuilder.DropColumn(
                name: "AttachmentStoragePath",
                table: "toa");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "toa");
        }
    }
}
