using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyExtendedFieldsAndContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AccountOwnerUserId",
                table: "com_company",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessTypeName",
                table: "com_company",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerStatus",
                table: "com_company",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapUrl",
                table: "com_company",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextRenewalDate",
                table: "com_company",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "com_company",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegisteredDate",
                table: "com_company",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesNote",
                table: "com_company",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatRegistered",
                table: "com_company",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Com_CompanyContact",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ContactType = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Com_CompanyContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Com_CompanyContact_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Com_CompanyContact_CompanyId",
                table: "Com_CompanyContact",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Com_CompanyContact");

            migrationBuilder.DropColumn(
                name: "AccountOwnerUserId",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "BusinessTypeName",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "CustomerStatus",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "MapUrl",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "NextRenewalDate",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "RegisteredDate",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "SalesNote",
                table: "com_company");

            migrationBuilder.DropColumn(
                name: "VatRegistered",
                table: "com_company");
        }
    }
}
