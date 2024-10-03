using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderDevelop.Migrations
{
    /// <inheritdoc />
    public partial class addFacililator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "facililatorID",
                table: "GoalTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "facililatorID",
                table: "GoalTasks");
        }
    }
}
