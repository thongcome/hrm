using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareBenefitTypeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wel_BenefitTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameTh = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    EntitlementMode = table.Column<int>(type: "int", nullable: false),
                    AnnualLimitAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    PerEventLimitAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    MaxClaimsPerYear = table.Column<int>(type: "int", nullable: true),
                    RequiresReceipt = table.Column<bool>(type: "bit", nullable: false),
                    MinServiceMonths = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wel_BenefitTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wel_BenefitTypes_CompanyId_Code",
                table: "Wel_BenefitTypes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wel_BenefitTypes");
        }
    }
}
