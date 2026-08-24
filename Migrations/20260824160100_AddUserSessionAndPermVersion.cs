using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionAndPermVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "permversion",
                table: "sc_user",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "sc_user_session",
                columns: table => new
                {
                    sessionid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    sessiontoken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ipaddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    useragent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    createddate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lastseendate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isrevoked = table.Column<bool>(type: "bit", nullable: false),
                    revokeddate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revokedby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_user_session", x => x.sessionid);
                    table.ForeignKey(
                        name: "FK_sc_user_session_sc_user_userid",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sc_user_session_userid",
                table: "sc_user_session",
                column: "userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sc_user_session");

            migrationBuilder.DropColumn(
                name: "permversion",
                table: "sc_user");
        }
    }
}
