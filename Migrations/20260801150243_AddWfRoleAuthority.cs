using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWfRoleAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "info_message",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_info_message", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_role_authority",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    orgid = table.Column<long>(type: "bigint", nullable: false),
                    posid = table.Column<long>(type: "bigint", nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_role_authority", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "info_message");

            migrationBuilder.DropTable(
                name: "wf_role_authority");
        }
    }
}
