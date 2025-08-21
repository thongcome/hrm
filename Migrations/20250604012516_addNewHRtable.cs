using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class addNewHRtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HRTaxRate",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    COOP_ID = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    nameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    minRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    maxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    taxLevelRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    taxAccumLevelRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    year = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    step = table.Column<int>(type: "int", nullable: false),
                    MODIFIED_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    MODIFIED_BY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRTaxRate", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HRTaxRate");
        }
    }
}
