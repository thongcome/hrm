using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddAdSsoScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AD/SSO scaffold (CEO, 2026-09-07): prepare structure for both
            // AD/LDAP bind auth and SSO/OIDC redirect auth — no real customer
            // AD/IdP to connect to yet, so nothing here changes behaviour for
            // any existing account (AuthProvider defaults to NULL = today's
            // local-password flow, unchanged).
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "sc_user",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sc_external_identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ScUserId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LinkedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_external_identity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sc_external_identity_Provider_ExternalKey",
                table: "sc_external_identity",
                columns: new[] { "Provider", "ExternalKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sc_external_identity");
            migrationBuilder.DropColumn(name: "AuthProvider", table: "sc_user");
        }
    }
}
