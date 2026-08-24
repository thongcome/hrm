using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sc_role_scope",
                columns: table => new
                {
                    scopeid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    scopetype = table.Column<int>(type: "int", nullable: false),
                    scopevalue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_role_scope", x => x.scopeid);
                    table.ForeignKey(
                        name: "FK_sc_role_scope_sc_role_roleid",
                        column: x => x.roleid,
                        principalTable: "sc_role",
                        principalColumn: "roleid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_scope_roleid",
                table: "sc_role_scope",
                column: "roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sc_role_scope");
        }
    }
}
