using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderDevelop.Migrations
{
    /// <inheritdoc />
    public partial class addGoalEmail_ID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerID",
                table: "GoalTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "GoalTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerID",
                table: "GoalTasks");

            migrationBuilder.DropColumn(
                name: "email",
                table: "GoalTasks");
        }
    }
}
