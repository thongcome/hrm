using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddScProgramRoleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sc_program_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    progpath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    cancreate = table.Column<bool>(type: "bit", nullable: false),
                    canread = table.Column<bool>(type: "bit", nullable: false),
                    canedit = table.Column<bool>(type: "bit", nullable: false),
                    candelete = table.Column<bool>(type: "bit", nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_program_role", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sc_program_role");
        }
    }
}
