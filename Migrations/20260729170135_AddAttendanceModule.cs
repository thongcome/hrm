using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Att_CompanySetting",
                columns: table => new
                {
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingMode = table.Column<int>(type: "int", nullable: false),
                    DefaultWorkStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    DefaultWorkEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    AllowWfh = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_CompanySetting", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "Att_Device",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DeviceType = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_Device", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Att_ShiftDefinition",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_ShiftDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Att_ImportBatch",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ImportedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ImportedRows = table.Column<int>(type: "int", nullable: false),
                    SkippedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_ImportBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_ImportBatch_Att_Device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Att_Device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Att_DailyAttendance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShiftDefinitionId = table.Column<long>(type: "bigint", nullable: true),
                    FirstIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkedMinutes = table.Column<int>(type: "int", nullable: true),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    IsEarlyLeave = table.Column<bool>(type: "bit", nullable: false),
                    IsAbsent = table.Column<bool>(type: "bit", nullable: false),
                    WorkLocation = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_DailyAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_DailyAttendance_Att_ShiftDefinition_ShiftDefinitionId",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "Att_ShiftDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Att_DailyAttendance_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Att_ShiftAssignment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_ShiftAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_ShiftAssignment_Att_ShiftDefinition_ShiftDefinitionId",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "Att_ShiftDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Att_ShiftAssignment_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Att_PunchLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    RawEmpCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    ImportBatchId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_PunchLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Att_PunchLog_Att_Device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Att_Device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Att_PunchLog_Att_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "Att_ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Att_PunchLog_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Att_DailyAttendance_HremployeeId_WorkDate",
                table: "Att_DailyAttendance",
                columns: new[] { "HremployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Att_DailyAttendance_ShiftDefinitionId",
                table: "Att_DailyAttendance",
                column: "ShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_ImportBatch_DeviceId",
                table: "Att_ImportBatch",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_PunchLog_DeviceId",
                table: "Att_PunchLog",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_PunchLog_HremployeeId",
                table: "Att_PunchLog",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_PunchLog_ImportBatchId",
                table: "Att_PunchLog",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Att_ShiftAssignment_HremployeeId_WorkDate",
                table: "Att_ShiftAssignment",
                columns: new[] { "HremployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Att_ShiftAssignment_ShiftDefinitionId",
                table: "Att_ShiftAssignment",
                column: "ShiftDefinitionId");

            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 7, "บันทึกเวลาเข้างาน", "Time Attendance", 1, true, 6, "ATT_ADMIN", true, "/att/settings", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 6, 7, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 6 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 7 });

            migrationBuilder.DropTable(
                name: "Att_CompanySetting");

            migrationBuilder.DropTable(
                name: "Att_DailyAttendance");

            migrationBuilder.DropTable(
                name: "Att_PunchLog");

            migrationBuilder.DropTable(
                name: "Att_ShiftAssignment");

            migrationBuilder.DropTable(
                name: "Att_ImportBatch");

            migrationBuilder.DropTable(
                name: "Att_ShiftDefinition");

            migrationBuilder.DropTable(
                name: "Att_Device");
        }
    }
}
