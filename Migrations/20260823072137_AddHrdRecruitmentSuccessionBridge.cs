using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddHrdRecruitmentSuccessionBridge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinYearsExperience",
                table: "Succ_KeyPosition",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rec_CandidateEducation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Major = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MajorSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Faculty = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Institute = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinishedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gpa = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsHonors = table.Column<bool>(type: "bit", nullable: false),
                    IsHighestDegree = table.Column<bool>(type: "bit", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_CandidateEducation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_CandidateExperience",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_CandidateExperience", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rec_CandidateEducation");

            migrationBuilder.DropTable(
                name: "Rec_CandidateExperience");

            migrationBuilder.DropColumn(
                name: "MinYearsExperience",
                table: "Succ_KeyPosition");
        }
    }
}
