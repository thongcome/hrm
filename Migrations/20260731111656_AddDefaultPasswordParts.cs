using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultPasswordParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultPasswordPart1",
                table: "Pay_PayslipSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPasswordPart2",
                table: "Pay_PayslipSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pay_PayslipSettings",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "DefaultPasswordPart1", "DefaultPasswordPart2" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPasswordPart1",
                table: "Pay_PayslipSettings");

            migrationBuilder.DropColumn(
                name: "DefaultPasswordPart2",
                table: "Pay_PayslipSettings");
        }
    }
}
