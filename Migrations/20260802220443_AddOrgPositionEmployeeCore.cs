using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgPositionEmployeeCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManpower",
                table: "com_organization",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SectionTypeCode",
                table: "com_organization",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubSectionTypeId",
                table: "com_organization",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "createby",
                table: "com_organization",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "createdate",
                table: "com_organization",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Com_SectionType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AbbName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AbbNameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
                    IsTopLevel = table.Column<bool>(type: "bit", nullable: false),
                    UpperSecType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Com_SectionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Com_SubSectionType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Com_SubSectionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_ContactPerson",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Relation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_ContactPerson", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_Education",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
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
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_Education", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_Experience",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_Experience", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_Family",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    CardId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    FamilyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrenameCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Religion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAlive = table.Column<bool>(type: "bit", nullable: false),
                    DeceasedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Career = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsChild = table.Column<bool>(type: "bit", nullable: false),
                    ChildStudying = table.Column<bool>(type: "bit", nullable: false),
                    ChildDeductionEligible = table.Column<bool>(type: "bit", nullable: false),
                    EduRefundEligible = table.Column<bool>(type: "bit", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_Family", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_Marriage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    MarriageNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MarriedAt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegisterDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BookNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SpouseName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SpouseAge = table.Column<int>(type: "int", nullable: true),
                    SpouseCareer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SpouseIncome = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ChildCount = table.Column<int>(type: "int", nullable: true),
                    ChildStudyingCount = table.Column<int>(type: "int", nullable: true),
                    ChildNotStudyingCount = table.Column<int>(type: "int", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_Marriage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_NameChangeHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    OldPrenameCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OldFirstName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OldLastName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewPrenameCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewFirstName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewLastName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DocNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RegisterDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_NameChangeHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pos_EmployeeType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_EmployeeType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pos_ExecType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EmployeeTypeId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AbbName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AbbNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsTopLevel = table.Column<bool>(type: "bit", nullable: false),
                    IsBoss = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_ExecType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pos_PositionSlot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PosCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeTypeId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AbbName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MinGraduate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MinSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    NormalSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    MaxSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    IsManpower = table.Column<bool>(type: "bit", nullable: false),
                    IsBoss = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pos_PositionSlot", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Com_SectionType");

            migrationBuilder.DropTable(
                name: "Com_SubSectionType");

            migrationBuilder.DropTable(
                name: "Hrd_ContactPerson");

            migrationBuilder.DropTable(
                name: "Hrd_Education");

            migrationBuilder.DropTable(
                name: "Hrd_Experience");

            migrationBuilder.DropTable(
                name: "Hrd_Family");

            migrationBuilder.DropTable(
                name: "Hrd_Marriage");

            migrationBuilder.DropTable(
                name: "Hrd_NameChangeHistory");

            migrationBuilder.DropTable(
                name: "Pos_EmployeeType");

            migrationBuilder.DropTable(
                name: "Pos_ExecType");

            migrationBuilder.DropTable(
                name: "Pos_PositionSlot");

            migrationBuilder.DropColumn(
                name: "IsManpower",
                table: "com_organization");

            migrationBuilder.DropColumn(
                name: "SectionTypeCode",
                table: "com_organization");

            migrationBuilder.DropColumn(
                name: "SubSectionTypeId",
                table: "com_organization");

            migrationBuilder.DropColumn(
                name: "createby",
                table: "com_organization");

            migrationBuilder.DropColumn(
                name: "createdate",
                table: "com_organization");
        }
    }
}
