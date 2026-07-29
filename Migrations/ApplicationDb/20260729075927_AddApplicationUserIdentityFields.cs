using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations.ApplicationDb
{
    // Hand-pruned: `dotnet ef migrations add` generated a full from-scratch
    // schema (CreateTable for every table) because ApplicationDbContext had
    // never had a migration applied before — every one of those tables
    // already exists (created years ago via HRMContext's own migration
    // history). The only real change here is 3 columns that were added to
    // ApplicationUser.cs in code but never migrated onto the live
    // AspNetUsers table: FirstName, LastName, userid.
    /// <inheritdoc />
    public partial class AddApplicationUserIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "userid",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "AspNetUsers");
        }
    }
}
