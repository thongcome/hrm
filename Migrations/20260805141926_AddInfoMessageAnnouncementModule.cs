using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddInfoMessageAnnouncementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "message",
                table: "info_message",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "info_message",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "info_message",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "info_message",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ModifiedByUserId",
                table: "info_message",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "info_message",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityLevel",
                table: "info_message",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Info_MessageReadLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InfoMessageId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    DocCenterId = table.Column<long>(type: "bigint", nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Info_MessageReadLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Info_MessageTarget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InfoMessageId = table.Column<long>(type: "bigint", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TargetHremployeeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Info_MessageTarget", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Info_MessageReadLog_InfoMessageId_HremployeeId",
                table: "Info_MessageReadLog",
                columns: new[] { "InfoMessageId", "HremployeeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Info_MessageReadLog");

            migrationBuilder.DropTable(
                name: "Info_MessageTarget");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "info_message");

            migrationBuilder.AlterColumn<string>(
                name: "message",
                table: "info_message",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
