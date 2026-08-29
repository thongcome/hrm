using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddAdhocPayItemAnomalyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnomalyNote",
                table: "Pay_AdhocPayItem",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAmountAnomalyFlagged",
                table: "Pay_AdhocPayItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnomalyNote",
                table: "Pay_AdhocPayItem");

            migrationBuilder.DropColumn(
                name: "IsAmountAnomalyFlagged",
                table: "Pay_AdhocPayItem");
        }
    }
}
