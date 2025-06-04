using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class addEmp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "sc_user",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                defaultValueSql: "(NULL)",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true,
                oldDefaultValueSql: "(NULL)");

            migrationBuilder.AlterColumn<string>(
                name: "NameEn",
                table: "employee",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "HRPayrollPayByRequest",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SEQ_NO = table.Column<int>(type: "int", precision: 3, nullable: false),
                    SALITEM_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ITEM_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Reason_Pay = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Period_Attend = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Approve_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OrgCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Acc_Code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    is_Tax_Cal = table.Column<bool>(type: "bit", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    SignFlag = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isFix = table.Column<bool>(type: "bit", nullable: true),
                    approve_date = table.Column<DateTime>(type: "DATE", nullable: true),
                    REMARK = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MODIFIED_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    MODIFIED_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRPayrollPayByRequest", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HRPayrollPayByRequest");

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "sc_user",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                defaultValueSql: "(NULL)",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldDefaultValueSql: "(NULL)");

            migrationBuilder.AlterColumn<string>(
                name: "NameEn",
                table: "employee",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }
    }
}
