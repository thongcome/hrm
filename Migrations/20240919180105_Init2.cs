using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderDevelop.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Activity",
                newName: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Orders",
                table: "Activity",
                newName: "Order");
        }
    }
}
