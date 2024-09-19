using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderDevelop.Migrations
{
    /// <inheritdoc />
    public partial class start2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "wolId",
                table: "GoalTasks",
                newName: "WolId");

            migrationBuilder.AlterColumn<string>(
                name: "Modby",
                table: "GoalTasks",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "GoalTasks",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreateBy",
                table: "GoalTasks",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.CreateTable(
                name: "WOLs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    theme = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WOLs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoalTasks_WolId",
                table: "GoalTasks",
                column: "WolId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalTasks_WOLs_WolId",
                table: "GoalTasks",
                column: "WolId",
                principalTable: "WOLs",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalTasks_WOLs_WolId",
                table: "GoalTasks");

            migrationBuilder.DropTable(
                name: "WOLs");

            migrationBuilder.DropIndex(
                name: "IX_GoalTasks_WolId",
                table: "GoalTasks");

            migrationBuilder.RenameColumn(
                name: "WolId",
                table: "GoalTasks",
                newName: "wolId");

            migrationBuilder.AlterColumn<string>(
                name: "Modby",
                table: "GoalTasks",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "GoalTasks",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "CreateBy",
                table: "GoalTasks",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}
