using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pay_PayItemType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameTh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DefaultSignFlag = table.Column<int>(type: "int", nullable: false),
                    IsSystemReserved = table.Column<bool>(type: "bit", nullable: false),
                    GLAccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayItemType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_PayrollRun",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PayrollPeriod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    PayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RunType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdjustmentOfRunId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CalculatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollRun_Pay_PayrollRun_AdjustmentOfRunId",
                        column: x => x.AdjustmentOfRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_ProvidentFundElection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeContributionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CompanyContributionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_ProvidentFundElection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_ProvidentFundElection_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_TaxBracket",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveYear = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    MinIncome = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    MaxIncome = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_TaxBracket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pay_BankFileExportBatch",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    BankFormatCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRecordCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_BankFileExportBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_BankFileExportBatch_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_GLExportBatch",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    ExportFormatCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GeneratedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_GLExportBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_GLExportBatch_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_PayrollEmployee",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    ProrationFactor = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    WorkingDaysInPeriod = table.Column<int>(type: "int", nullable: false),
                    ActualWorkingDays = table.Column<int>(type: "int", nullable: false),
                    GrossEarnings = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SocialSecurityAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ProvidentFundEmployeeAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ProvidentFundCompanyAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IsNegativeNetPayFlag = table.Column<bool>(type: "bit", nullable: false),
                    IsExcluded = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    BankBranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BankAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollEmployee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollEmployee_HREMPLOYEE_HremployeeId",
                        column: x => x.HremployeeId,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollEmployee_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_GLExportEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GLExportBatchId = table.Column<long>(type: "bigint", nullable: false),
                    GLAccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CostCenterCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_GLExportEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_GLExportEntry_Pay_GLExportBatch_GLExportBatchId",
                        column: x => x.GLExportBatchId,
                        principalTable: "Pay_GLExportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_BankFileExportLine",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankFileExportBatchId = table.Column<long>(type: "bigint", nullable: false),
                    PayrollEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    BankBranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BankAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_BankFileExportLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_BankFileExportLine_Pay_BankFileExportBatch_BankFileExportBatchId",
                        column: x => x.BankFileExportBatchId,
                        principalTable: "Pay_BankFileExportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pay_BankFileExportLine_Pay_PayrollEmployee_PayrollEmployeeId",
                        column: x => x.PayrollEmployeeId,
                        principalTable: "Pay_PayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pay_PayrollAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<long>(type: "bigint", nullable: false),
                    PayrollEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollAuditLog_Pay_PayrollEmployee_PayrollEmployeeId",
                        column: x => x.PayrollEmployeeId,
                        principalTable: "Pay_PayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollAuditLog_Pay_PayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "Pay_PayrollRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_PayrollLineItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PayItemTypeId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceRefTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceRefId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SignFlag = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SeqNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_PayrollLineItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollLineItem_Pay_PayItemType_PayItemTypeId",
                        column: x => x.PayItemTypeId,
                        principalTable: "Pay_PayItemType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pay_PayrollLineItem_Pay_PayrollEmployee_PayrollEmployeeId",
                        column: x => x.PayrollEmployeeId,
                        principalTable: "Pay_PayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pay_Payslip",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PdfStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PdfSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsPublishedToEmployee = table.Column<bool>(type: "bit", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pay_Payslip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pay_Payslip_Pay_PayrollEmployee_PayrollEmployeeId",
                        column: x => x.PayrollEmployeeId,
                        principalTable: "Pay_PayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 1, 0, "BASE", 1, null, true, true, "Base Salary", "เงินเดือนฐาน", 1 },
                    { 2, 0, "OT", 1, null, true, true, "Overtime", "ค่าล่วงเวลา", 2 },
                    { 3, 0, "ALLOWANCE", 1, null, true, false, "Allowance", "เบี้ยเลี้ยง/เงินเพิ่มประจำ", 3 },
                    { 4, 1, "SSO", -1, null, true, true, "Social Security", "ประกันสังคม", 4 },
                    { 5, 1, "PF", -1, null, true, true, "Provident Fund (Employee)", "กองทุนสำรองเลี้ยงชีพ (พนักงาน)", 5 },
                    { 6, 1, "TAX", -1, null, true, true, "Withholding Tax", "ภาษีหัก ณ ที่จ่าย", 6 },
                    { 7, 1, "LOAN", -1, null, true, true, "Loan Deduction", "หักเงินกู้", 7 },
                    { 8, 2, "ADJUST", 1, null, true, false, "Special Adjustment", "ปรับปรุงพิเศษ", 8 }
                });

            migrationBuilder.InsertData(
                table: "Pay_TaxBracket",
                columns: new[] { "Id", "EffectiveYear", "IsActive", "MaxIncome", "MinIncome", "RatePercent", "Step" },
                values: new object[,]
                {
                    { 1, 2026, true, 150000m, 0m, 0m, 1 },
                    { 2, 2026, true, 300000m, 150000m, 5m, 2 },
                    { 3, 2026, true, 500000m, 300000m, 10m, 3 },
                    { 4, 2026, true, 750000m, 500000m, 15m, 4 },
                    { 5, 2026, true, 1000000m, 750000m, 20m, 5 },
                    { 6, 2026, true, 2000000m, 1000000m, 25m, 6 },
                    { 7, 2026, true, 5000000m, 2000000m, 30m, 7 },
                    { 8, 2026, true, null, 5000000m, 35m, 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pay_BankFileExportBatch_PayrollRunId",
                table: "Pay_BankFileExportBatch",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_BankFileExportLine_BankFileExportBatchId",
                table: "Pay_BankFileExportLine",
                column: "BankFileExportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_BankFileExportLine_PayrollEmployeeId",
                table: "Pay_BankFileExportLine",
                column: "PayrollEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_GLExportBatch_PayrollRunId",
                table: "Pay_GLExportBatch",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_GLExportEntry_GLExportBatchId",
                table: "Pay_GLExportEntry",
                column: "GLExportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollAuditLog_PayrollEmployeeId",
                table: "Pay_PayrollAuditLog",
                column: "PayrollEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollAuditLog_PayrollRunId",
                table: "Pay_PayrollAuditLog",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollEmployee_HremployeeId",
                table: "Pay_PayrollEmployee",
                column: "HremployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollEmployee_PayrollRunId_HremployeeId",
                table: "Pay_PayrollEmployee",
                columns: new[] { "PayrollRunId", "HremployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollLineItem_PayItemTypeId",
                table: "Pay_PayrollLineItem",
                column: "PayItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollLineItem_PayrollEmployeeId",
                table: "Pay_PayrollLineItem",
                column: "PayrollEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollRun_AdjustmentOfRunId",
                table: "Pay_PayrollRun",
                column: "AdjustmentOfRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType",
                table: "Pay_PayrollRun",
                columns: new[] { "CompanyId", "PayrollPeriod", "RunType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pay_Payslip_PayrollEmployeeId",
                table: "Pay_Payslip",
                column: "PayrollEmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pay_ProvidentFundElection_HremployeeId",
                table: "Pay_ProvidentFundElection",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pay_BankFileExportLine");

            migrationBuilder.DropTable(
                name: "Pay_GLExportEntry");

            migrationBuilder.DropTable(
                name: "Pay_PayrollAuditLog");

            migrationBuilder.DropTable(
                name: "Pay_PayrollLineItem");

            migrationBuilder.DropTable(
                name: "Pay_Payslip");

            migrationBuilder.DropTable(
                name: "Pay_ProvidentFundElection");

            migrationBuilder.DropTable(
                name: "Pay_TaxBracket");

            migrationBuilder.DropTable(
                name: "Pay_BankFileExportBatch");

            migrationBuilder.DropTable(
                name: "Pay_GLExportBatch");

            migrationBuilder.DropTable(
                name: "Pay_PayItemType");

            migrationBuilder.DropTable(
                name: "Pay_PayrollEmployee");

            migrationBuilder.DropTable(
                name: "Pay_PayrollRun");
        }
    }
}
